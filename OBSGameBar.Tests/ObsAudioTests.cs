using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Services;

namespace OBSGameBar.Tests
{
    [TestClass]
    public class ObsAudioTests
    {
        [TestMethod]
        public void TestInputMuteStateChanged_UpdatesAudioInput()
        {
            var mockWs = new MockObsWebSocketService();
            var state = new ObsState();
            var audioService = new ObsAudioService(mockWs, state);

            var micInput = new AudioInputModel
            {
                InputName = "Mic/Aux",
                IsMuted = false,
                VolumeDb = -6.0f
            };
            state.AudioInputs.Add(micInput);

            mockWs.RaiseSimulatedEvent("InputMuteStateChanged", "{\"inputName\":\"Mic/Aux\",\"inputMuted\":true}");

            Assert.IsTrue(micInput.IsMuted);
            Assert.AreEqual("\uE74F", micInput.MuteIconGlyph);
        }

        [TestMethod]
        public void TestInputVolumeChanged_UpdatesVolumeDb()
        {
            var mockWs = new MockObsWebSocketService();
            var state = new ObsState();
            var audioService = new ObsAudioService(mockWs, state);

            var desktopInput = new AudioInputModel
            {
                InputName = "Desktop Audio",
                IsMuted = false,
                VolumeDb = -18.0f
            };
            state.AudioInputs.Add(desktopInput);

            mockWs.RaiseSimulatedEvent("InputVolumeChanged", "{\"inputName\":\"Desktop Audio\",\"inputVolumeDb\":-12.4}");

            Assert.AreEqual(-12.4f, desktopInput.VolumeDb, 0.05f);
            Assert.AreEqual("-12.4 dB", desktopInput.VolumeDisplayString);
        }

        [TestMethod]
        public async Task TestSetVolumeDbDebounced_CoalescesRapidChanges()
        {
            var mockWs = new MockObsWebSocketService { Status = ObsConnectionStatus.Connected };
            var state = new ObsState();
            using (var audioService = new ObsAudioService(mockWs, state))
            {
                var input = new AudioInputModel { InputName = "Mic", VolumeDb = 0f };
                state.AudioInputs.Add(input);

                // Simulate rapid user dragging slider
                audioService.SetVolumeDbDebounced("Mic", -5.0f);
                audioService.SetVolumeDbDebounced("Mic", -10.0f);
                audioService.SetVolumeDbDebounced("Mic", -15.0f);

                // Local state is immediately updated for fluid UI
                Assert.AreEqual(-15.0f, input.VolumeDb, 0.01f);

                // Wait for debounce timer (50ms + buffer)
                await Task.Delay(150);

                // Should have sent only ONE request with the final value (-15.0f)
                var volumeRequests = mockWs.SentRequests.FindAll(r => r.requestType == "SetInputVolume");
                Assert.AreEqual(1, volumeRequests.Count);
            }
        }

        [TestMethod]
        public async Task TestToggleInputMuteAsync_UpdatesLocalStateCaseInsensitive()
        {
            var mockWs = new MockObsWebSocketService { Status = ObsConnectionStatus.Connected };
            mockWs.SetMockResponse("ToggleInputMute", new { inputMuted = true });
            var state = new ObsState();
            using (var audioService = new ObsAudioService(mockWs, state))
            {
                var input = new AudioInputModel { InputName = "Media", IsMuted = false };
                state.AudioInputs.Add(input);

                await audioService.ToggleInputMuteAsync("media");

                Assert.IsTrue(input.IsMuted);
                Assert.AreEqual("\uE74F", input.MuteIconGlyph);
            }
        }

        [TestMethod]
        public void TestInputMuteStateChanged_CaseInsensitive_UpdatesModel()
        {
            var mockWs = new MockObsWebSocketService();
            var state = new ObsState();
            var audioService = new ObsAudioService(mockWs, state);

            var mediaInput = new AudioInputModel
            {
                InputName = "Media",
                IsMuted = false
            };
            state.AudioInputs.Add(mediaInput);

            mockWs.RaiseSimulatedEvent("InputMuteStateChanged", "{\"inputName\":\"media\",\"inputMuted\":true}");

            Assert.IsTrue(mediaInput.IsMuted);
            Assert.AreEqual("\uE74F", mediaInput.MuteIconGlyph);
        }

        [TestMethod]
        public async Task TestRefreshAudioInputsAsync_FiltersByActiveSceneAndGlobalInputs()
        {
            var mockWs = new MockObsWebSocketService { Status = ObsConnectionStatus.Connected };
            var state = new ObsState { CurrentProgramScene = "Scene" };
            var audioService = new ObsAudioService(mockWs, state);

            // Mock Special Inputs (Global Devices)
            mockWs.SetMockResponse("GetSpecialInputs", new
            {
                desktop1 = "Desktop Audio",
                mic1 = "Mic/Aux",
                desktop2 = (string)null,
                mic2 = (string)null
            });

            // Mock Scene Items in "Scene" (includes "Discord Audio" and "Media", but NOT "Media 2")
            mockWs.SetMockResponse("GetSceneItemList", new
            {
                sceneItems = new[]
                {
                    new { sourceName = "Discord Audio", isGroup = false },
                    new { sourceName = "Media", isGroup = false }
                }
            });

            // Mock All Inputs in OBS
            mockWs.SetMockResponse("GetInputList", new
            {
                inputs = new[]
                {
                    new { inputName = "Desktop Audio", inputKind = "wasapi_output_capture" },
                    new { inputName = "Mic/Aux", inputKind = "wasapi_input_capture" },
                    new { inputName = "Discord Audio", inputKind = "wasapi_process_output_capture" },
                    new { inputName = "Media", inputKind = "ffmpeg_source" },
                    new { inputName = "Media 2", inputKind = "ffmpeg_source" }
                }
            });

            mockWs.SetMockResponse("GetInputVolume", new { inputVolumeDb = 0.0, inputVolumeMul = 1.0 });
            mockWs.SetMockResponse("GetInputMute", new { inputMuted = false });

            await audioService.RefreshAudioInputsAsync("Scene");

            // Should contain Desktop Audio, Mic/Aux, Discord Audio, Media (4 items). Media 2 must be excluded!
            Assert.AreEqual(4, state.AudioInputs.Count);
            Assert.IsNotNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Desktop Audio"));
            Assert.IsNotNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Mic/Aux"));
            Assert.IsNotNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Discord Audio"));
            Assert.IsNotNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Media"));
            Assert.IsNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Media 2"));
        }

        [TestMethod]
        public async Task TestRefreshAudioInputsAsync_SceneSwitchUpdatesMixer()
        {
            var mockWs = new MockObsWebSocketService { Status = ObsConnectionStatus.Connected };
            var state = new ObsState { CurrentProgramScene = "Scene" };
            var audioService = new ObsAudioService(mockWs, state);

            mockWs.SetMockResponse("GetSpecialInputs", new
            {
                desktop1 = "Desktop Audio",
                mic1 = "Mic/Aux"
            });

            mockWs.SetMockResponse("GetInputList", new
            {
                inputs = new[]
                {
                    new { inputName = "Desktop Audio", inputKind = "wasapi_output_capture" },
                    new { inputName = "Mic/Aux", inputKind = "wasapi_input_capture" },
                    new { inputName = "Media", inputKind = "ffmpeg_source" },
                    new { inputName = "Media 2", inputKind = "ffmpeg_source" }
                }
            });

            mockWs.SetMockResponse("GetInputVolume", new { inputVolumeDb = 0.0, inputVolumeMul = 1.0 });
            mockWs.SetMockResponse("GetInputMute", new { inputMuted = false });

            // 1. Initial on "Scene" -> Scene Items contain "Media"
            mockWs.SetMockResponse("GetSceneItemList", new
            {
                sceneItems = new[] { new { sourceName = "Media", isGroup = false } }
            });
            await audioService.RefreshAudioInputsAsync("Scene");
            Assert.AreEqual(3, state.AudioInputs.Count);
            Assert.IsNotNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Media"));
            Assert.IsNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Media 2"));

            // 2. Switch to "Scene 2" -> Scene Items contain "Media 2"
            mockWs.SetMockResponse("GetSceneItemList", new
            {
                sceneItems = new[] { new { sourceName = "Media 2", isGroup = false } }
            });
            await audioService.RefreshAudioInputsAsync("Scene 2");
            Assert.AreEqual(3, state.AudioInputs.Count);
            Assert.IsNotNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Media 2"));
            Assert.IsNull(state.AudioInputs.FirstOrDefault(a => a.InputName == "Media"));
        }
    }
}
