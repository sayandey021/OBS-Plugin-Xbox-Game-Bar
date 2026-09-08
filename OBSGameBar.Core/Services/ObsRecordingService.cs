using System;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Core.Services
{
    public class ObsRecordingService
    {
        private readonly IObsWebSocketService _webSocketService;
        private readonly ObsState _state;
        private System.Threading.Timer _statusTimer;
        private int _polling;

        public event Action<string> NotificationRequested;

        public ObsRecordingService(IObsWebSocketService webSocketService, ObsState state)
        {
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
            _state = state ?? throw new ArgumentNullException(nameof(state));

            _webSocketService.EventReceived += OnEventReceived;
        }

        /// <summary>
        /// Polls GetRecordStatus every second so the timecode/duration stays live.
        /// OBS RecordStateChanged events do not carry duration, so polling is required.
        /// </summary>
        private void StartStatusPolling()
        {
            if (Interlocked.Exchange(ref _polling, 1) == 1) return;

            _statusTimer = new System.Threading.Timer(
                async _ => await PollOnceAsync().ConfigureAwait(false),
                null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void StopStatusPolling()
        {
            if (Interlocked.Exchange(ref _polling, 0) == 0) return;

            _statusTimer?.Dispose();
            _statusTimer = null;
        }

        private async Task PollOnceAsync()
        {
            if (!_webSocketService.IsConnected)
            {
                StopStatusPolling();
                return;
            }

            if (_state.IsRecording)
            {
                await RefreshStatusAsync().ConfigureAwait(false);
            }
            else
            {
                StopStatusPolling();
            }
        }

        public async Task RefreshStatusAsync()
        {
            if (!_webSocketService.IsConnected) return;

            try
            {
                var resp = await _webSocketService.SendRequestAsync("GetRecordStatus").ConfigureAwait(false);
                if (resp.HasValue && resp.Value.ValueKind == JsonValueKind.Object)
                {
                    UpdateFromRecordJson(resp.Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsRecordingService] RefreshStatus error: {ex.Message}");
            }
        }

        public async Task StartRecordAsync()
        {
            await _webSocketService.SendRequestAsync("StartRecord").ConfigureAwait(false);
        }

        public async Task StopRecordAsync()
        {
            await _webSocketService.SendRequestAsync("StopRecord").ConfigureAwait(false);
        }

        public async Task ToggleRecordAsync()
        {
            await _webSocketService.SendRequestAsync("ToggleRecord").ConfigureAwait(false);
        }

        public async Task PauseRecordAsync()
        {
            await _webSocketService.SendRequestAsync("PauseRecord").ConfigureAwait(false);
        }

        public async Task ResumeRecordAsync()
        {
            await _webSocketService.SendRequestAsync("ResumeRecord").ConfigureAwait(false);
        }

        public async Task ToggleRecordPauseAsync()
        {
            await _webSocketService.SendRequestAsync("ToggleRecordPause").ConfigureAwait(false);
        }

        private void OnEventReceived(object sender, ObsEventReceivedEventArgs e)
        {
            if (e.EventType == "RecordStateChanged")
            {
                if (e.EventData.TryGetProperty("outputActive", out var activeProp))
                {
                    bool nowActive = activeProp.GetBoolean();
                    if (nowActive) { StartStatusPolling(); } else { StopStatusPolling(); }
                    bool wasRecording = _state.IsRecording;
                    _state.IsRecording = nowActive;
                    if (!wasRecording && _state.IsRecording)
                    {
                        NotificationRequested?.Invoke("Recording started");
                    }
                    else if (wasRecording && !_state.IsRecording)
                    {
                        NotificationRequested?.Invoke("Recording stopped");
                    }
                }

                if (e.EventData.TryGetProperty("outputState", out var stateProp))
                {
                    string stateStr = stateProp.GetString();
                    if (stateStr == "OBS_WEBSOCKET_OUTPUT_PAUSED")
                    {
                        _state.IsRecordingPaused = true;
                    }
                    else if (stateStr == "OBS_WEBSOCKET_OUTPUT_RESUMED" || stateStr == "OBS_WEBSOCKET_OUTPUT_STARTED")
                    {
                        _state.IsRecordingPaused = false;
                    }
                    else if (stateStr == "OBS_WEBSOCKET_OUTPUT_STOPPED")
                    {
                        _state.IsRecordingPaused = false;
                        _state.RecordingDuration = TimeSpan.Zero;
                    }
                }
            }
        }

        private void UpdateFromRecordJson(JsonElement data)
        {
            if (data.TryGetProperty("outputActive", out var activeProp))
            {
                _state.IsRecording = activeProp.GetBoolean();
            }

            if (data.TryGetProperty("outputPaused", out var pausedProp))
            {
                _state.IsRecordingPaused = pausedProp.GetBoolean();
            }

            if (data.TryGetProperty("outputDuration", out var durProp))
            {
                long ms = durProp.GetInt64();
                _state.RecordingDuration = TimeSpan.FromMilliseconds(ms);
            }

            if (data.TryGetProperty("outputTimecode", out var tcProp))
            {
                _state.RecordingTimecode = tcProp.GetString();
            }

            // Keep the timecode live while a recording is already in progress (e.g. at connect)
            if (_state.IsRecording)
            {
                StartStatusPolling();
            }
        }
    }
}



