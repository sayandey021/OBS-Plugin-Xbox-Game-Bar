using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Services;

namespace OBSGameBar.Tests
{
    public class MockObsWebSocketService : IObsWebSocketService
    {
        public ObsConnectionStatus Status { get; set; } = ObsConnectionStatus.Disconnected;
        public bool IsConnected => Status == ObsConnectionStatus.Connected;

        public event EventHandler<ObsEventReceivedEventArgs> EventReceived;
        public event EventHandler<ObsConnectionStateChangedEventArgs> StateChanged;

        public List<(string requestType, object requestData)> SentRequests { get; } =
            new List<(string requestType, object requestData)>();

        public Dictionary<string, JsonElement?> QueuedResponses { get; } =
            new Dictionary<string, JsonElement?>();

        public void SetMockResponse(string requestType, object data)
        {
            if (data == null)
            {
                QueuedResponses[requestType] = null;
                return;
            }
            string json = JsonSerializer.Serialize(data);
            using (var doc = JsonDocument.Parse(json))
            {
                QueuedResponses[requestType] = doc.RootElement.Clone();
            }
        }

        public bool ShouldFailConnect { get; set; }
        public bool ShouldAuthFail { get; set; }

        public Task ConnectAsync(string host, int port, string password = null, CancellationToken cancellationToken = default)
        {
            if (ShouldAuthFail)
            {
                Status = ObsConnectionStatus.AuthFailed;
                StateChanged?.Invoke(this, new ObsConnectionStateChangedEventArgs(ObsConnectionStatus.Connecting, ObsConnectionStatus.AuthFailed, "Auth Failed"));
                throw new UnauthorizedAccessException("Auth Failed");
            }

            if (ShouldFailConnect)
            {
                Status = ObsConnectionStatus.Error;
                StateChanged?.Invoke(this, new ObsConnectionStateChangedEventArgs(ObsConnectionStatus.Connecting, ObsConnectionStatus.Error, "Connect Failed"));
                throw new InvalidOperationException("Connect Failed");
            }

            var old = Status;
            Status = ObsConnectionStatus.Connected;
            StateChanged?.Invoke(this, new ObsConnectionStateChangedEventArgs(old, ObsConnectionStatus.Connected, "Connected"));
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            var old = Status;
            Status = ObsConnectionStatus.Disconnected;
            StateChanged?.Invoke(this, new ObsConnectionStateChangedEventArgs(old, ObsConnectionStatus.Disconnected, "Disconnected"));
            return Task.CompletedTask;
        }

        public Task<JsonElement?> SendRequestAsync(string requestType, object requestData = null, TimeSpan? timeout = null)
        {
            SentRequests.Add((requestType, requestData));

            if (QueuedResponses.TryGetValue(requestType, out var resp))
            {
                return Task.FromResult(resp);
            }

            return Task.FromResult<JsonElement?>(null);
        }

        public void RaiseSimulatedEvent(string eventType, string jsonEventData)
        {
            using (var doc = JsonDocument.Parse(jsonEventData))
            {
                EventReceived?.Invoke(this, new ObsEventReceivedEventArgs(eventType, 0, doc.RootElement.Clone()));
            }
        }

        public void RaiseSimulatedDisconnect()
        {
            var old = Status;
            Status = ObsConnectionStatus.Disconnected;
            StateChanged?.Invoke(this, new ObsConnectionStateChangedEventArgs(old, ObsConnectionStatus.Disconnected, "Disconnected"));
        }

        public void Dispose() { }
    }
}
