using System;
using System.Text.Json;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.ViewModels;

namespace OBSGameBar.Core.Services
{
    public class ObsStreamService
    {
        private readonly IObsWebSocketService _webSocketService;
        private readonly ObsState _state;

        public event Action<string> NotificationRequested;

        public ObsStreamService(IObsWebSocketService webSocketService, ObsState state)
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
                var resp = await _webSocketService.SendRequestAsync("GetStreamStatus").ConfigureAwait(false);
                if (resp.HasValue && resp.Value.ValueKind == JsonValueKind.Object)
                {
                    UpdateFromStatusJson(resp.Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsStreamService] RefreshStatus error: {ex.Message}");
            }
        }

        public async Task StartStreamAsync()
        {
            await _webSocketService.SendRequestAsync("StartStream").ConfigureAwait(false);
        }

        public async Task StopStreamAsync()
        {
            await _webSocketService.SendRequestAsync("StopStream").ConfigureAwait(false);
        }

        public async Task ToggleStreamAsync()
        {
            await _webSocketService.SendRequestAsync("ToggleStream").ConfigureAwait(false);
        }

        private void OnEventReceived(object sender, ObsEventReceivedEventArgs e)
        {
            if (e.EventType == "StreamStateChanged")
            {
                if (e.EventData.TryGetProperty("outputActive", out var activeProp))
                {
                    bool wasStreaming = _state.IsStreaming;
                    _state.IsStreaming = activeProp.GetBoolean();
                    if (!wasStreaming && _state.IsStreaming)
                    {
                        NotificationRequested?.Invoke("Stream started");
                    }
                    else if (wasStreaming && !_state.IsStreaming)
                    {
                        NotificationRequested?.Invoke("Stream stopped");
                    }
                }
            }
            else if (e.EventType == "StreamStatusUpdate")
            {
                UpdateFromStatusJson(e.EventData);
            }
        }

        private void UpdateFromStatusJson(JsonElement data)
        {
            ViewModelBase.Log("[ObsStreamService] UpdateFromStatusJson started");
            if (data.TryGetProperty("outputActive", out var activeProp))
            {
                bool active = activeProp.GetBoolean();
                ViewModelBase.Log($"[ObsStreamService] setting IsStreaming={active}");
                _state.IsStreaming = active;
                ViewModelBase.Log($"[ObsStreamService] IsStreaming set complete");
            }

            if (data.TryGetProperty("outputDuration", out var durProp))
            {
                long ms = durProp.GetInt64();
                ViewModelBase.Log($"[ObsStreamService] setting StreamDuration={ms}ms");
                _state.StreamDuration = TimeSpan.FromMilliseconds(ms);
            }

            if (data.TryGetProperty("outputKbitsPerSec", out var kbpsProp))
            {
                double kbps = kbpsProp.GetDouble();
                ViewModelBase.Log($"[ObsStreamService] setting StreamKbitsPerSec={kbps}");
                _state.StreamKbitsPerSec = kbps;
            }

            if (data.TryGetProperty("outputSkippedFrames", out var skippedProp))
            {
                _state.StreamDroppedFrames = skippedProp.GetInt64();
            }

            if (data.TryGetProperty("outputTotalFrames", out var totalProp))
            {
                _state.StreamTotalFrames = totalProp.GetInt64();
                if (_state.StreamTotalFrames > 0)
                {
                    _state.StreamDroppedFramesPercent = ((double)_state.StreamDroppedFrames / _state.StreamTotalFrames) * 100.0;
                }
            }
            ViewModelBase.Log("[ObsStreamService] UpdateFromStatusJson completed");
        }
    }
}
