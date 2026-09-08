using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Core.Services
{
    public class ObsEventReceivedEventArgs : EventArgs
    {
        public string EventType { get; }
        public uint EventIntent { get; }
        public JsonElement EventData { get; }

        public ObsEventReceivedEventArgs(string eventType, uint eventIntent, JsonElement eventData)
        {
            EventType = eventType;
            EventIntent = eventIntent;
            EventData = eventData;
        }
    }

    public class ObsConnectionStateChangedEventArgs : EventArgs
    {
        public ObsConnectionStatus OldStatus { get; }
        public ObsConnectionStatus NewStatus { get; }
        public string Message { get; }

        public ObsConnectionStateChangedEventArgs(ObsConnectionStatus oldStatus, ObsConnectionStatus newStatus, string message)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
            Message = message;
        }
    }

    public interface IObsWebSocketService : IDisposable
    {
        event EventHandler<ObsEventReceivedEventArgs> EventReceived;
        event EventHandler<ObsConnectionStateChangedEventArgs> StateChanged;

        ObsConnectionStatus Status { get; }
        bool IsConnected { get; }

        Task ConnectAsync(string host, int port, string password = null, CancellationToken cancellationToken = default);
        Task DisconnectAsync();
        Task<JsonElement?> SendRequestAsync(string requestType, object requestData = null, TimeSpan? timeout = null);
    }
}
