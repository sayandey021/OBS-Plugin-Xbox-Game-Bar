using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Core.Services
{
    public class ObsSourceService
    {
        private readonly IObsWebSocketService _webSocketService;
        private readonly ObsState _state;

        public ObsSourceService(IObsWebSocketService webSocketService, ObsState state)
        {
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
            _state = state ?? throw new ArgumentNullException(nameof(state));

            _webSocketService.EventReceived += OnEventReceived;
        }

        public async Task RefreshSourcesForCurrentSceneAsync(string sceneName = null)
        {
            if (!_webSocketService.IsConnected) return;

            string targetScene = sceneName;
            if (string.IsNullOrEmpty(targetScene))
            {
                targetScene = _state.IsStudioMode
                    ? (!string.IsNullOrEmpty(_state.CurrentPreviewScene) ? _state.CurrentPreviewScene : _state.CurrentProgramScene)
                    : _state.CurrentProgramScene;
            }

            if (string.IsNullOrEmpty(targetScene)) return;

            try
            {
                var resp = await _webSocketService.SendRequestAsync("GetSceneItemList", new
                {
                    sceneName = targetScene
                }).ConfigureAwait(false);

                if (resp.HasValue && resp.Value.TryGetProperty("sceneItems", out var itemsArray) &&
                    itemsArray.ValueKind == JsonValueKind.Array)
                {
                    var newSources = new List<SourceModel>();
                    foreach (var item in itemsArray.EnumerateArray())
                    {
                        long itemId = item.GetProperty("sceneItemId").GetInt64();
                        string sourceName = item.GetProperty("sourceName").GetString();
                        bool isEnabled = item.TryGetProperty("sceneItemEnabled", out var enProp) && enProp.GetBoolean();
                        string inputKind = item.TryGetProperty("inputKind", out var kindProp) ? kindProp.GetString() : string.Empty;

                        newSources.Add(new SourceModel
                        {
                            SceneItemId = itemId,
                            SourceName = sourceName,
                            IsEnabled = isEnabled,
                            InputKind = inputKind,
                            SceneName = targetScene
                        });
                    }
                    _state.UpdateCurrentSceneSources(newSources);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsSourceService] RefreshSources error: {ex.Message}");
            }
        }

        public async Task SetSourceVisibilityAsync(string sceneName, long sceneItemId, bool enabled)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                sceneName = _state.IsStudioMode
                    ? (!string.IsNullOrEmpty(_state.CurrentPreviewScene) ? _state.CurrentPreviewScene : _state.CurrentProgramScene)
                    : _state.CurrentProgramScene;
            }

            if (string.IsNullOrEmpty(sceneName)) return;

            try
            {
                await _webSocketService.SendRequestAsync("SetSceneItemEnabled", new
                {
                    sceneName = sceneName,
                    sceneItemId = sceneItemId,
                    sceneItemEnabled = enabled
                }).ConfigureAwait(false);

                // Optimistically update local source
                foreach (var src in _state.CurrentSceneSources)
                {
                    if (src.SceneItemId == sceneItemId)
                    {
                        src.IsEnabled = enabled;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ObsSourceService] SetSourceVisibility error: {ex.Message}");
            }
        }

        private void OnEventReceived(object sender, ObsEventReceivedEventArgs e)
        {
            switch (e.EventType)
            {
                case "SceneItemEnableStateChanged":
                    if (e.EventData.TryGetProperty("sceneItemId", out var idProp) &&
                        e.EventData.TryGetProperty("sceneItemEnabled", out var enProp))
                    {
                        long changedId = idProp.GetInt64();
                        bool enabled = enProp.GetBoolean();
                        string eventSceneName = e.EventData.TryGetProperty("sceneName", out var scProp) ? scProp.GetString() : null;

                        foreach (var src in _state.CurrentSceneSources)
                        {
                            if (src.SceneItemId == changedId &&
                                (string.IsNullOrEmpty(src.SceneName) || string.IsNullOrEmpty(eventSceneName) || string.Equals(src.SceneName, eventSceneName, StringComparison.OrdinalIgnoreCase)))
                            {
                                src.IsEnabled = enabled;
                                break;
                            }
                        }
                    }
                    break;

                case "SceneItemCreated":
                case "SceneItemRemoved":
                case "SceneItemListReindexed":
                    _ = RefreshSourcesForCurrentSceneAsync();
                    break;
            }
        }
    }
}
