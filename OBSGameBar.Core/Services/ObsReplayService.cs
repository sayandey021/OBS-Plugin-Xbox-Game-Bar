using System;
using System.Text.Json;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Core.Services
{
    public class ObsReplayService
    {
        private readonly IObsWebSocketService _webSocketService;
        private readonly ObsState _state;

        public event Action<string> NotificationRequested;

        public ObsReplayService(IObsWebSocketService webSocketService, ObsState state)
        {
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
            _state = state ?? throw new ArgumentNullException(nameof(state));

            _webSocketService.EventReceived += OnEventReceived;
        }

        public async Task RefreshStatusAsync()
        {
            if (!_webSocketService.IsConnected) return;

            try
            {
                var resp = await _webSocketService.SendRequestAsync("GetReplayBufferStatus").ConfigureAwait(false);
                if (resp.HasValue && resp.Value.ValueKind == JsonValueKind.Object)
                {
                    if (resp.Value.TryGetProperty("outputActive", out var activeProp))
                    {
                        _state.IsReplayBufferActive = activeProp.GetBoolean();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsReplayService] RefreshStatus error: {ex.Message}");
            }
        }

        public async Task StartReplayBufferAsync()
        {
            await _webSocketService.SendRequestAsync("StartReplayBuffer").ConfigureAwait(false);
        }

        public async Task StopReplayBufferAsync()
        {
            await _webSocketService.SendRequestAsync("StopReplayBuffer").ConfigureAwait(false);
        }

        public async Task ToggleReplayBufferAsync()
        {
            await _webSocketService.SendRequestAsync("ToggleReplayBuffer").ConfigureAwait(false);
        }

        public async Task SaveReplayBufferAsync()
        {
            _state.IsReplayBufferSaving = true;
            try
            {
                await _webSocketService.SendRequestAsync("SaveReplayBuffer").ConfigureAwait(false);
            }
            finally
            {
                // ReplayBufferSaved event will clear saving, or reset after 1s
                _ = Task.Delay(1000).ContinueWith(_ => _state.IsReplayBufferSaving = false);
            }
        }

        private void OnEventReceived(object sender, ObsEventReceivedEventArgs e)
        {
            if (e.EventType == "ReplayBufferStateChanged")
            {
                if (e.EventData.TryGetProperty("outputActive", out var activeProp))
                {
                    _state.IsReplayBufferActive = activeProp.GetBoolean();
                }
            }
            else if (e.EventType == "ReplayBufferSaved")
            {
                _state.IsReplayBufferSaving = false;
                string path = string.Empty;
                if (e.EventData.TryGetProperty("savedReplayPath", out var pathProp))
                {
                    path = pathProp.GetString();
                    _state.LastReplaySavedPath = path;
                }
                NotificationRequested?.Invoke(!string.IsNullOrEmpty(path) ? $"Replay saved: {System.IO.Path.GetFileName(path)}" : "Replay saved!");
            }
        }
    }
}
