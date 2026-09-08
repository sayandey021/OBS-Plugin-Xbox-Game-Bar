using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Services;
using OBSGameBar.Core.ViewModels;

namespace OBSGameBar.Tests
{
    [TestClass]
    public class ObsStateSyncTests
    {
        private MockObsWebSocketService _mockWs;
        private ObsState _state;
        private ObsConnectionManager _connMgr;
        private ObsStreamService _streamService;
        private ObsRecordingService _recordingService;
        private ObsReplayService _replayService;
        private ObsSceneService _sceneService;
        private ObsSourceService _sourceService;
        private ObsAudioService _audioService;
        private ConnectionSettings _settings;
        private MainWidgetViewModel _viewModel;

        [TestInitialize]
        public void Setup()
        {
            _mockWs = new MockObsWebSocketService();
            _state = new ObsState();
            _settings = new ConnectionSettings();
            _connMgr = new ObsConnectionManager(_mockWs);
            _streamService = new ObsStreamService(_mockWs, _state);
            _recordingService = new ObsRecordingService(_mockWs, _state);
            _replayService = new ObsReplayService(_mockWs, _state);
            _sceneService = new ObsSceneService(_mockWs, _state);
            _sourceService = new ObsSourceService(_mockWs, _state);
            _audioService = new ObsAudioService(_mockWs, _state);

            _viewModel = new MainWidgetViewModel(
                _state,
                _connMgr,
                _streamService,
                _recordingService,
                _replayService,
                _sceneService,
                _sourceService,
                _audioService,
                _settings,
                _mockWs);
        }

        [TestMethod]
        public void TestStreamStateChanged_UpdatesStateAndNotification()
        {
            string notified = null;
            _viewModel.NotificationRequested += msg => notified = msg;

            _mockWs.RaiseSimulatedEvent("StreamStateChanged", "{\"outputActive\":true,\"outputState\":\"OBS_WEBSOCKET_OUTPUT_STARTED\"}");

            Assert.IsTrue(_state.IsStreaming);
            Assert.AreEqual("Stream started", notified);

            _mockWs.RaiseSimulatedEvent("StreamStateChanged", "{\"outputActive\":false,\"outputState\":\"OBS_WEBSOCKET_OUTPUT_STOPPED\"}");

            Assert.IsFalse(_state.IsStreaming);
            Assert.AreEqual("Stream stopped", notified);
        }

        [TestMethod]
        public void TestStreamStatusUpdate_ComputesBitrateAndDroppedFramesPercentage()
        {
            _mockWs.RaiseSimulatedEvent("StreamStatusUpdate", "{\"outputActive\":true,\"outputDuration\":60000,\"outputKbitsPerSec\":6200.0,\"outputSkippedFrames\":2,\"outputTotalFrames\":10000}");

            Assert.IsTrue(_state.IsStreaming);
            Assert.AreEqual(6200.0, _state.StreamKbitsPerSec);
            Assert.AreEqual("6.2 Mbps", _state.StreamBitrateDisplay);
            Assert.AreEqual(2, _state.StreamDroppedFrames);
            Assert.AreEqual(10000, _state.StreamTotalFrames);
            Assert.AreEqual(0.02, _state.StreamDroppedFramesPercent, 0.001);
            Assert.AreEqual("0.02% dropped", _state.StreamDroppedFramesPercentDisplay);
        }

        [TestMethod]
        public void TestRecordStateChanged_PauseAndResume_UpdatesState()
        {
            _mockWs.RaiseSimulatedEvent("RecordStateChanged", "{\"outputActive\":true,\"outputState\":\"OBS_WEBSOCKET_OUTPUT_STARTED\"}");
            Assert.IsTrue(_state.IsRecording);
            Assert.IsFalse(_state.IsRecordingPaused);

            _mockWs.RaiseSimulatedEvent("RecordStateChanged", "{\"outputActive\":true,\"outputState\":\"OBS_WEBSOCKET_OUTPUT_PAUSED\"}");
            Assert.IsTrue(_state.IsRecording);
            Assert.IsTrue(_state.IsRecordingPaused);

            _mockWs.RaiseSimulatedEvent("RecordStateChanged", "{\"outputActive\":true,\"outputState\":\"OBS_WEBSOCKET_OUTPUT_RESUMED\"}");
            Assert.IsTrue(_state.IsRecording);
            Assert.IsFalse(_state.IsRecordingPaused);
        }

        [TestMethod]
        public void TestReplayBufferSaved_UpdatesStateAndNotification()
        {
            string notification = null;
            _viewModel.NotificationRequested += msg => notification = msg;

            _mockWs.RaiseSimulatedEvent("ReplayBufferSaved", "{\"savedReplayPath\":\"C:\\\\Videos\\\\Replay_2026.mp4\"}");

            Assert.IsFalse(_state.IsReplayBufferSaving);
            Assert.AreEqual("C:\\Videos\\Replay_2026.mp4", _state.LastReplaySavedPath);
            Assert.IsTrue(notification.Contains("Replay saved"));
        }

        [TestMethod]
        public void TestCurrentProgramSceneChanged_UpdatesCurrentSceneAndFlags()
        {
            _state.Scenes.Add(new SceneModel { Name = "Gaming", IsActive = true });
            _state.Scenes.Add(new SceneModel { Name = "BRB", IsActive = false });

            _mockWs.RaiseSimulatedEvent("CurrentProgramSceneChanged", "{\"sceneName\":\"BRB\"}");

            Assert.AreEqual("BRB", _state.CurrentProgramScene);
            Assert.IsFalse(_state.Scenes[0].IsActive);
            Assert.IsTrue(_state.Scenes[1].IsActive);
        }

        [TestMethod]
        public void TestSceneItemEnableStateChanged_UpdatesSourceVisibility()
        {
            var source = new SourceModel { SceneItemId = 42, SourceName = "Webcam", IsEnabled = false };
            _state.CurrentSceneSources.Add(source);

            _mockWs.RaiseSimulatedEvent("SceneItemEnableStateChanged", "{\"sceneName\":\"Gaming\",\"sceneItemId\":42,\"sceneItemEnabled\":true}");

            Assert.IsTrue(source.IsEnabled);
        }

        [TestMethod]
        public void TestSetSourceVisibilityCommand_SendsExplicitVisibility()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            var source = new SourceModel { SceneItemId = 3, SourceName = "Window Capture", IsEnabled = false, SceneName = "Scene" };
            _state.CurrentSceneSources.Add(source);

            _viewModel.SetSourceVisibilityCommand.Execute((source, true));

            bool sent = _mockWs.SentRequests.Exists(r => r.requestType == "SetSceneItemEnabled");
            Assert.IsTrue(sent);
            Assert.IsTrue(source.IsEnabled);
        }

        [TestMethod]
        public void TestToggleSourceVisibilityCommand_InvertsVisibility()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            var source = new SourceModel { SceneItemId = 3, SourceName = "Window Capture", IsEnabled = true, SceneName = "Scene" };
            _state.CurrentSceneSources.Add(source);

            _viewModel.ToggleSourceVisibilityCommand.Execute(source);

            bool sent = _mockWs.SentRequests.Exists(r => r.requestType == "SetSceneItemEnabled");
            Assert.IsTrue(sent);
            Assert.IsFalse(source.IsEnabled);
        }

        [TestMethod]
        public async Task TestQuickAction_SaveReplay_SendsRequest()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            var replayAction = new QuickActionItem(2, QuickActionType.SaveReplay);

            await _viewModel.HandleQuickActionAsync(replayAction);

            bool sent = _mockWs.SentRequests.Exists(r => r.requestType == "SaveReplayBuffer");
            Assert.IsTrue(sent);
        }

        [TestMethod]
        public async Task TestQuickAction_ToggleVirtualCam_SendsRequest()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            var vcamAction = new QuickActionItem(7, QuickActionType.ToggleVirtualCam);

            await _viewModel.HandleQuickActionAsync(vcamAction);

            bool sent = _mockWs.SentRequests.Exists(r => r.requestType == "ToggleVirtualCam");
            Assert.IsTrue(sent);
        }

        [TestMethod]
        public async Task TestRefreshScenes_ReversesScenesToMatchObsDockOrder()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            _mockWs.SetMockResponse("GetStudioModeEnabled", new { studioModeEnabled = true });
            // OBS returns scenes where index 0 is bottom ("Scene 2"), index 1 is top ("Scene")
            _mockWs.SetMockResponse("GetSceneList", new
            {
                currentProgramSceneName = "Scene 2",
                currentPreviewSceneName = "Scene",
                scenes = new[]
                {
                    new { sceneIndex = 0, sceneName = "Scene 2" },
                    new { sceneIndex = 1, sceneName = "Scene" }
                }
            });

            await _sceneService.RefreshScenesAsync();

            // Reversed order should put "Scene" first and "Scene 2" second (matching dock top-to-bottom)
            Assert.AreEqual(2, _state.Scenes.Count);
            Assert.AreEqual("Scene", _state.Scenes[0].Name);
            Assert.AreEqual("Scene 2", _state.Scenes[1].Name);
            Assert.IsTrue(_state.Scenes[0].IsPreview);
            Assert.IsTrue(_state.Scenes[1].IsActive);
        }

        [TestMethod]
        public void TestSwitchSceneCommand_NormalMode_CallsSetCurrentProgramScene()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            _state.IsStudioMode = false;
            var scene = new SceneModel { Name = "BRB" };

            _viewModel.SwitchSceneCommand.Execute(scene);

            bool sent = _mockWs.SentRequests.Exists(r => r.requestType == "SetCurrentProgramScene");
            Assert.IsTrue(sent);
        }

        [TestMethod]
        public void TestSwitchSceneCommand_StudioMode_StagesPreview()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            _state.IsStudioMode = true;
            _state.CurrentPreviewScene = "Scene";
            var scene = new SceneModel { Name = "Scene 2", IsPreview = false };

            _viewModel.SwitchSceneCommand.Execute(scene);

            bool sent = _mockWs.SentRequests.Exists(r => r.requestType == "SetCurrentPreviewScene");
            Assert.IsTrue(sent);
        }

        [TestMethod]
        public void TestSwitchSceneCommand_StudioMode_AlwaysStagesPreview_NeverAutoTransitions()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            _state.IsStudioMode = true;
            _state.CurrentPreviewScene = "Scene 2";
            var scene = new SceneModel { Name = "Scene 2", IsPreview = true };

            _viewModel.SwitchSceneCommand.Execute(scene);

            bool transSent = _mockWs.SentRequests.Exists(r => r.requestType == "TriggerStudioModeTransition");
            bool previewSent = _mockWs.SentRequests.Exists(r => r.requestType == "SetCurrentPreviewScene");
            Assert.IsFalse(transSent, "Clicking a scene in Studio Mode must never auto-transition to Program.");
            Assert.IsTrue(previewSent, "Clicking a scene in Studio Mode must always stage it to Preview.");
        }

        [TestMethod]
        public void TestCurrentPreviewSceneChanged_UpdatesPreviewFlagsAndState()
        {
            _state.IsStudioMode = true;
            var s1 = new SceneModel { Name = "Scene", IsPreview = false, IsStudioMode = true };
            var s2 = new SceneModel { Name = "Scene 2", IsPreview = true, IsStudioMode = true };
            _state.Scenes.Add(s1);
            _state.Scenes.Add(s2);

            _mockWs.RaiseSimulatedEvent("CurrentPreviewSceneChanged", "{\"sceneName\":\"Scene\"}");

            Assert.AreEqual("Scene", _state.CurrentPreviewScene);
            Assert.IsTrue(s1.IsPreview);
            Assert.AreEqual("StudioPreview", s1.SceneState);
            Assert.IsFalse(s2.IsPreview);
            Assert.AreEqual("Inactive", s2.SceneState);
        }

        [TestMethod]
        public void TestSceneTransitionEnded_StudioMode_RefreshesProgramAndPreviewScenes()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            _state.IsStudioMode = true;
            _mockWs.SetMockResponse("GetCurrentProgramScene", new { currentProgramSceneName = "Scene 2" });
            _mockWs.SetMockResponse("GetCurrentPreviewScene", new { currentPreviewSceneName = "Scene" });

            _mockWs.RaiseSimulatedEvent("SceneTransitionEnded", "{}");

            bool progQueried = _mockWs.SentRequests.Exists(r => r.requestType == "GetCurrentProgramScene");
            bool prevQueried = _mockWs.SentRequests.Exists(r => r.requestType == "GetCurrentPreviewScene");
            Assert.IsTrue(progQueried);
            Assert.IsTrue(prevQueried);
        }

        [TestMethod]
        public void TestStudioMode_BadgeVisibilityAndHeader()
        {
            _state.IsStudioMode = true;
            Assert.AreEqual("Scene sources (Preview)", _state.SceneSourcesHeader);

            var scene = new SceneModel { Name = "Scene", IsStudioMode = true, IsActive = true, IsPreview = false };
            Assert.IsTrue(scene.ShowLiveBadge);
            Assert.IsFalse(scene.ShowPreviewBadge);
            Assert.AreEqual("StudioLive", scene.SceneState);

            scene.IsPreview = true;
            Assert.IsTrue(scene.ShowLiveBadge);
            Assert.IsTrue(scene.ShowPreviewBadge);
            Assert.AreEqual("StudioLivePreview", scene.SceneState);

            scene.IsActive = false;
            Assert.IsFalse(scene.ShowLiveBadge);
            Assert.IsTrue(scene.ShowPreviewBadge);
            Assert.AreEqual("StudioPreview", scene.SceneState);

            _state.IsStudioMode = false;
            Assert.AreEqual("Scene sources", _state.SceneSourcesHeader);
            scene.IsStudioMode = false;
            scene.IsActive = true;
            Assert.IsFalse(scene.ShowLiveBadge);
            Assert.IsFalse(scene.ShowPreviewBadge);
            Assert.AreEqual("NormalActive", scene.SceneState);
        }

        [TestMethod]
        public void TestToggleStudioModeCommand_SendsSetStudioModeEnabled()
        {
            _mockWs.Status = ObsConnectionStatus.Connected;
            _state.IsStudioMode = true;

            _viewModel.ToggleStudioModeCommand.Execute(null);

            bool sent = _mockWs.SentRequests.Exists(r => r.requestType == "SetStudioModeEnabled");
            Assert.IsTrue(sent);
        }
    }
}
