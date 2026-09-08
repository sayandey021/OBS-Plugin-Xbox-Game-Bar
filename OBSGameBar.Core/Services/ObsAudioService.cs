using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Core.Services
{
    public class ObsAudioService : IDisposable
    {
        private readonly IObsWebSocketService _webSocketService;
        private readonly ObsState _state;
        private readonly ConcurrentDictionary<string, (float db, Timer timer)> _debounceTimers =
            new ConcurrentDictionary<string, (float, Timer)>();
        private readonly object _timerLock = new object();

        public ObsAudioService(IObsWebSocketService webSocketService, ObsState state)
        {
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
            _state = state ?? throw new ArgumentNullException(nameof(state));

            _webSocketService.EventReceived += OnEventReceived;
        }

        public async Task RefreshAudioInputsAsync(string activeSceneName = null)
        {
            if (!_webSocketService.IsConnected) return;

            try
            {
                // 1. Resolve active scene name
                string targetScene = activeSceneName;
                if (string.IsNullOrEmpty(targetScene))
                {
                    targetScene = _state.IsStudioMode && !string.IsNullOrEmpty(_state.CurrentPreviewScene)
                        ? _state.CurrentPreviewScene
                        : _state.CurrentProgramScene;
                }

                // 2. Fetch Global Audio Devices (desktop1, desktop2, mic1..4)
                var globalInputs = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var specialResp = await _webSocketService.SendRequestAsync("GetSpecialInputs").ConfigureAwait(false);
                    if (specialResp.HasValue && specialResp.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in specialResp.Value.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.String)
                            {
                                string val = prop.Value.GetString();
                                if (!string.IsNullOrWhiteSpace(val))
                                {
                                    globalInputs.Add(val);
                                }
                            }
                        }
                    }
                }
                catch { }

                // 3. Fetch Scene Items for the target scene
                var sceneInputs = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(targetScene))
                {
                    try
                    {
                        var sceneItemsResp = await _webSocketService.SendRequestAsync("GetSceneItemList", new { sceneName = targetScene }).ConfigureAwait(false);
                        if (sceneItemsResp.HasValue && sceneItemsResp.Value.TryGetProperty("sceneItems", out var itemsArray) && itemsArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in itemsArray.EnumerateArray())
                            {
                                if (item.TryGetProperty("sourceName", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                                {
                                    string srcName = nameProp.GetString();
                                    if (!string.IsNullOrWhiteSpace(srcName))
                                    {
                                        sceneInputs.Add(srcName);
                                    }
                                }

                                // Handle group items if present
                                if (item.TryGetProperty("isGroup", out var isGroupProp) && isGroupProp.ValueKind == JsonValueKind.True)
                                {
                                    if (item.TryGetProperty("sourceName", out var groupNameProp))
                                    {
                                        try
                                        {
                                            var groupResp = await _webSocketService.SendRequestAsync("GetGroupSceneItemList", new { sceneName = groupNameProp.GetString() }).ConfigureAwait(false);
                                            if (groupResp.HasValue && groupResp.Value.TryGetProperty("sceneItems", out var groupItems) && groupItems.ValueKind == JsonValueKind.Array)
                                            {
                                                foreach (var gItem in groupItems.EnumerateArray())
                                                {
                                                    if (gItem.TryGetProperty("sourceName", out var gNameProp) && gNameProp.ValueKind == JsonValueKind.String)
                                                    {
                                                        sceneInputs.Add(gNameProp.GetString());
                                                    }
                                                }
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // 4. Query all inputs from OBS
                var resp = await _webSocketService.SendRequestAsync("GetInputList").ConfigureAwait(false);
                if (resp.HasValue && resp.Value.TryGetProperty("inputs", out var inputsArray) &&
                    inputsArray.ValueKind == JsonValueKind.Array)
                {
                    var activeInputs = new System.Collections.Generic.List<AudioInputModel>();
                    bool hasFiltering = globalInputs.Count > 0 || sceneInputs.Count > 0;

                    foreach (var input in inputsArray.EnumerateArray())
                    {
                        string name = input.GetProperty("inputName").GetString();
                        string kind = input.TryGetProperty("inputKind", out var kindProp) ? kindProp.GetString() : string.Empty;

                        // If filtering is available, include only global audio devices OR inputs present in the active scene
                        if (hasFiltering && !globalInputs.Contains(name) && !sceneInputs.Contains(name))
                        {
                            continue;
                        }

                        // Query volume and mute state
                        float volDb = 0f;
                        float volMul = 1f;
                        bool isMuted = false;

                        try
                        {
                            var volResp = await _webSocketService.SendRequestAsync("GetInputVolume", new { inputName = name }).ConfigureAwait(false);
                            if (volResp.HasValue)
                            {
                                if (volResp.Value.TryGetProperty("inputVolumeDb", out var dbProp))
                                    volDb = (float)dbProp.GetDouble();
                                if (volResp.Value.TryGetProperty("inputVolumeMul", out var mulProp))
                                    volMul = (float)mulProp.GetDouble();
                            }
                        }
                        catch { }

                        try
                        {
                            var muteResp = await _webSocketService.SendRequestAsync("GetInputMute", new { inputName = name }).ConfigureAwait(false);
                            if (muteResp.HasValue && muteResp.Value.TryGetProperty("inputMuted", out var muteProp))
                            {
                                isMuted = muteProp.GetBoolean();
                            }
                        }
                        catch { }

                        activeInputs.Add(new AudioInputModel
                        {
                            InputName = name,
                            InputKind = kind,
                            VolumeDb = volDb,
                            VolumeMul = volMul,
                            IsMuted = isMuted
                        });
                    }

                    _state.UpdateAudioInputs(activeInputs);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsAudioService] RefreshAudioInputs error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets volume with 50ms debouncing / coalescing to avoid flooding WebSocket while dragging sliders.
        /// </summary>
        public void SetVolumeDbDebounced(string inputName, float db)
        {
            if (string.IsNullOrEmpty(inputName)) return;

            // Immediately update the local state model for smooth UI responsiveness
            foreach (var input in _state.AudioInputs)
            {
                if (string.Equals(input.InputName, inputName, StringComparison.OrdinalIgnoreCase))
                {
                    input.VolumeDb = db;
                    break;
                }
            }

            lock (_timerLock)
            {
                if (_debounceTimers.TryGetValue(inputName, out var existing))
                {
                    existing.timer.Dispose();
                }

                Timer newTimer = null;
                newTimer = new Timer(async _ =>
                {
                    try
                    {
                        if (_debounceTimers.TryRemove(inputName, out var entry))
                        {
                            entry.timer.Dispose();
                            await SendSetVolumeDbAsync(inputName, entry.db).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ObsAudioService] SetVolumeDb failed: {ex.Message}");
                    }
                }, null, 50, Timeout.Infinite);

                _debounceTimers[inputName] = (db, newTimer);
            }
        }

        public async Task SendSetVolumeDbAsync(string inputName, float db)
        {
            if (!_webSocketService.IsConnected) return;

            await _webSocketService.SendRequestAsync("SetInputVolume", new
            {
                inputName = inputName,
                inputVolumeDb = db
            }).ConfigureAwait(false);
        }

        public async Task ToggleInputMuteAsync(string inputName)
        {
            if (!_webSocketService.IsConnected) return;

            var resp = await _webSocketService.SendRequestAsync("ToggleInputMute", new
            {
                inputName = inputName
            }).ConfigureAwait(false);

            if (resp.HasValue && resp.Value.TryGetProperty("inputMuted", out var mProp))
            {
                bool muted = mProp.GetBoolean();
                UpdateLocalMuteState(inputName, muted);
            }
        }

        public async Task SetInputMuteAsync(string inputName, bool muted)
        {
            if (!_webSocketService.IsConnected) return;

            await _webSocketService.SendRequestAsync("SetInputMute", new
            {
                inputName = inputName,
                inputMuted = muted
            }).ConfigureAwait(false);

            UpdateLocalMuteState(inputName, muted);
        }

        private void UpdateLocalMuteState(string inputName, bool muted)
        {
            foreach (var input in _state.AudioInputs)
            {
                if (string.Equals(input.InputName, inputName, StringComparison.OrdinalIgnoreCase))
                {
                    input.IsMuted = muted;
                    break;
                }
            }
        }

        private void OnEventReceived(object sender, ObsEventReceivedEventArgs e)
        {
            switch (e.EventType)
            {
                case "InputVolumeChanged":
                    if (e.EventData.TryGetProperty("inputName", out var nameProp))
                    {
                        string name = nameProp.GetString();
                        // If we are currently debouncing a slider change for this input, don't overwrite user's finger position
                        if (_debounceTimers.ContainsKey(name)) return;

                        if (e.EventData.TryGetProperty("inputVolumeDb", out var dbProp))
                        {
                            float db = (float)dbProp.GetDouble();
                            foreach (var input in _state.AudioInputs)
                            {
                                if (string.Equals(input.InputName, name, StringComparison.OrdinalIgnoreCase))
                                {
                                    input.VolumeDb = db;
                                    break;
                                }
                            }
                        }
                    }
                    break;

                case "InputMuteStateChanged":
                    if (e.EventData.TryGetProperty("inputName", out var mNameProp) &&
                        e.EventData.TryGetProperty("inputMuted", out var mutedProp))
                    {
                        string name = mNameProp.GetString();
                        bool muted = mutedProp.GetBoolean();
                        UpdateLocalMuteState(name, muted);
                    }
                    break;

                case "InputCreated":
                case "InputRemoved":
                case "InputNameChanged":
                case "SceneItemCreated":
                case "SceneItemRemoved":
                case "SceneItemListReindexed":
                    _ = RefreshAudioInputsAsync();
                    break;
            }
        }

        public void Dispose()
        {
            _webSocketService.EventReceived -= OnEventReceived;
            lock (_timerLock)
            {
                foreach (var kvp in _debounceTimers)
                {
                    kvp.Value.timer.Dispose();
                }
                _debounceTimers.Clear();
            }
        }
    }
}
