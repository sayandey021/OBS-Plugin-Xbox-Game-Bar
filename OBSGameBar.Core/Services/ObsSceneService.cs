using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Core.Services
{
    public class ObsSceneService
    {
        private readonly IObsWebSocketService _webSocketService;
        private readonly ObsState _state;
        private readonly Action<string> _onSceneChangedCallback;

        public ObsSceneService(IObsWebSocketService webSocketService, ObsState state, Action<string> onSceneChangedCallback = null)
        {
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _onSceneChangedCallback = onSceneChangedCallback;

            _webSocketService.EventReceived += OnEventReceived;
        }

        public async Task RefreshScenesAsync()
        {
            if (!_webSocketService.IsConnected) return;

            try
            {
                // Studio mode check
                try
                {
                    var studioResp = await _webSocketService.SendRequestAsync("GetStudioModeEnabled").ConfigureAwait(false);
                    if (studioResp.HasValue && studioResp.Value.TryGetProperty("studioModeEnabled", out var smProp))
                    {
                        _state.IsStudioMode = smProp.GetBoolean();
                    }
                }
                catch { }

                var resp = await _webSocketService.SendRequestAsync("GetSceneList").ConfigureAwait(false);
                if (resp.HasValue && resp.Value.ValueKind == JsonValueKind.Object)
                {
                    string currentProg = null;
                    string currentPrev = null;

                    if (resp.Value.TryGetProperty("currentProgramSceneName", out var progProp))
                    {
                        currentProg = progProp.GetString();
                        _state.CurrentProgramScene = currentProg;
                    }

                    if (resp.Value.TryGetProperty("currentPreviewSceneName", out var prevProp))
                    {
                        currentPrev = prevProp.GetString();
                        _state.CurrentPreviewScene = currentPrev;
                    }

                    // Fallback to GetCurrentProgramScene if missing from GetSceneList
                    if (string.IsNullOrEmpty(currentProg))
                    {
                        try
                        {
                            var progResp = await _webSocketService.SendRequestAsync("GetCurrentProgramScene").ConfigureAwait(false);
                            if (progResp.HasValue && progResp.Value.TryGetProperty("currentProgramSceneName", out var cpProp))
                            {
                                currentProg = cpProp.GetString();
                                _state.CurrentProgramScene = currentProg;
                            }
                        }
                        catch { }
                    }

                    // Fallback to GetCurrentPreviewScene if missing and Studio Mode is active
                    if (_state.IsStudioMode && string.IsNullOrEmpty(currentPrev))
                    {
                        try
                        {
                            var prevResp = await _webSocketService.SendRequestAsync("GetCurrentPreviewScene").ConfigureAwait(false);
                            if (prevResp.HasValue && prevResp.Value.TryGetProperty("currentPreviewSceneName", out var cprevProp))
                            {
                                currentPrev = cprevProp.GetString();
                                _state.CurrentPreviewScene = currentPrev;
                            }
                        }
                        catch { }
                    }
                    else if (!_state.IsStudioMode)
                    {
                        _state.CurrentPreviewScene = string.Empty;
                    }

                    if (resp.Value.TryGetProperty("scenes", out var scenesArray) && scenesArray.ValueKind == JsonValueKind.Array)
                    {
                        // In OBS WebSocket v5, scenes are indexed starting from the bottom (0 = bottom of dock).
                        // Reversing the list matches OBS Studio's visual top-to-bottom dock order.
                        var rawList = new List<JsonElement>();
                        foreach (var sceneElem in scenesArray.EnumerateArray())
                        {
                            rawList.Add(sceneElem);
                        }
                        rawList.Reverse();

                        var newScenes = new List<SceneModel>();
                        int idx = 0;
                        foreach (var sceneElem in rawList)
                        {
                            string sceneName = string.Empty;
                            if (sceneElem.TryGetProperty("sceneName", out var nameProp))
                            {
                                sceneName = nameProp.GetString();
                            }
                            else if (sceneElem.ValueKind == JsonValueKind.String)
                            {
                                sceneName = sceneElem.GetString();
                            }

                            if (!string.IsNullOrEmpty(sceneName))
                            {
                                newScenes.Add(new SceneModel
                                {
                                    Name = sceneName,
                                    Index = idx++,
                                    IsActive = sceneName == currentProg,
                                    IsPreview = _state.IsStudioMode && (sceneName == _state.CurrentPreviewScene),
                                    IsStudioMode = _state.IsStudioMode
                                });
                            }
                        }
                        _state.UpdateScenes(newScenes);
                    }

                    // In Studio Mode, display sources for the staged Preview scene; otherwise for the Program scene
                    string targetSourceScene = _state.IsStudioMode ? (_state.CurrentPreviewScene ?? currentProg) : currentProg;
                    if (!string.IsNullOrEmpty(targetSourceScene))
                    {
                        _onSceneChangedCallback?.Invoke(targetSourceScene);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsSceneService] RefreshScenes error: {ex.Message}");
            }
        }

        public async Task RefreshStudioModeScenesAsync()
        {
            if (!_webSocketService.IsConnected || !_state.IsStudioMode) return;

            try
            {
                var progTask = _webSocketService.SendRequestAsync("GetCurrentProgramScene");
                var prevTask = _webSocketService.SendRequestAsync("GetCurrentPreviewScene");
                await Task.WhenAll(progTask, prevTask).ConfigureAwait(false);

                var progResp = await progTask.ConfigureAwait(false);
                var prevResp = await prevTask.ConfigureAwait(false);

                if (progResp.HasValue && progResp.Value.TryGetProperty("currentProgramSceneName", out var cpProp))
                {
                    string progScene = cpProp.GetString();
                    _state.CurrentProgramScene = progScene;
                    UpdateSceneActiveFlags(progScene);
                }

                if (prevResp.HasValue && prevResp.Value.TryGetProperty("currentPreviewSceneName", out var prevProp))
                {
                    string prevScene = prevProp.GetString();
                    _state.CurrentPreviewScene = prevScene;
                    UpdateScenePreviewFlags(prevScene);
                }

                string targetSourceScene = _state.CurrentPreviewScene ?? _state.CurrentProgramScene;
                if (!string.IsNullOrEmpty(targetSourceScene))
                {
                    _onSceneChangedCallback?.Invoke(targetSourceScene);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsSceneService] RefreshStudioModeScenes error: {ex.Message}");
            }
        }

        public async Task SetCurrentProgramSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            await _webSocketService.SendRequestAsync("SetCurrentProgramScene", new
            {
                sceneName = sceneName
            }).ConfigureAwait(false);

            _state.CurrentProgramScene = sceneName;
            UpdateSceneActiveFlags(sceneName);
            if (!_state.IsStudioMode)
            {
                _onSceneChangedCallback?.Invoke(sceneName);
            }
        }

        public async Task SetCurrentPreviewSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            await _webSocketService.SendRequestAsync("SetCurrentPreviewScene", new
            {
                sceneName = sceneName
            }).ConfigureAwait(false);

            _state.CurrentPreviewScene = sceneName;
            UpdateScenePreviewFlags(sceneName);
            if (_state.IsStudioMode)
            {
                _onSceneChangedCallback?.Invoke(sceneName);
            }
        }

        public async Task TriggerStudioModeTransitionAsync()
        {
            await _webSocketService.SendRequestAsync("TriggerStudioModeTransition").ConfigureAwait(false);
        }

        public async Task SetStudioModeEnabledAsync(bool enabled)
        {
            await _webSocketService.SendRequestAsync("SetStudioModeEnabled", new
            {
                studioModeEnabled = enabled
            }).ConfigureAwait(false);

            _state.IsStudioMode = enabled;
            if (!enabled)
            {
                _state.CurrentPreviewScene = string.Empty;
                UpdateScenePreviewFlags(null);
            }
            await RefreshScenesAsync().ConfigureAwait(false);
        }

        public async Task ToggleStudioModeAsync()
        {
            await SetStudioModeEnabledAsync(!_state.IsStudioMode).ConfigureAwait(false);
        }

        private void OnEventReceived(object sender, ObsEventReceivedEventArgs e)
        {
            switch (e.EventType)
            {
                case "CurrentProgramSceneChanged":
                    if (e.EventData.TryGetProperty("sceneName", out var nameProp))
                    {
                        string sceneName = nameProp.GetString();
                        _state.CurrentProgramScene = sceneName;
                        UpdateSceneActiveFlags(sceneName);
                        if (!_state.IsStudioMode)
                        {
                            _onSceneChangedCallback?.Invoke(sceneName);
                        }
                        else
                        {
                            _ = RefreshStudioModeScenesAsync();
                        }
                    }
                    break;

                case "CurrentPreviewSceneChanged":
                    if (e.EventData.TryGetProperty("sceneName", out var prevProp))
                    {
                        string prevName = prevProp.GetString();
                        _state.CurrentPreviewScene = prevName;
                        UpdateScenePreviewFlags(prevName);
                        if (_state.IsStudioMode)
                        {
                            _onSceneChangedCallback?.Invoke(prevName);
                        }
                    }
                    break;

                case "SceneTransitionEnded":
                    if (_state.IsStudioMode)
                    {
                        _ = RefreshStudioModeScenesAsync();
                    }
                    break;

                case "StudioModeStateChanged":
                    if (e.EventData.TryGetProperty("studioModeEnabled", out var smProp))
                    {
                        bool smEnabled = smProp.GetBoolean();
                        _state.IsStudioMode = smEnabled;
                        if (!smEnabled)
                        {
                            _state.CurrentPreviewScene = string.Empty;
                            UpdateScenePreviewFlags(null);
                        }
                        _ = RefreshScenesAsync();
                    }
                    break;

                case "SceneListChanged":
                case "SceneCreated":
                case "SceneRemoved":
                case "SceneNameChanged":
                case "CurrentSceneCollectionChanged":
                    _ = RefreshScenesAsync();
                    break;
            }
        }

        private void UpdateSceneActiveFlags(string activeScene)
        {
            foreach (var s in _state.Scenes)
            {
                s.IsActive = !string.IsNullOrEmpty(activeScene) && (s.Name == activeScene);
            }
        }

        private void UpdateScenePreviewFlags(string previewScene)
        {
            foreach (var s in _state.Scenes)
            {
                s.IsPreview = !string.IsNullOrEmpty(previewScene) && (s.Name == previewScene);
            }
        }
    }
}
