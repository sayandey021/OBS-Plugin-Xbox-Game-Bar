using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Services;

namespace OBSGameBar.Core.ViewModels
{
    public class MainWidgetViewModel : ViewModelBase
    {
        private readonly ObsState _state;
        private readonly IObsConnectionManager _connectionManager;
        private readonly ObsStreamService _streamService;
        private readonly ObsRecordingService _recordingService;
        private readonly ObsReplayService _replayService;
        private readonly ObsSceneService _sceneService;
        private readonly ObsSourceService _sourceService;
        private readonly ObsAudioService _audioService;
        private readonly IObsWebSocketService _webSocketService;
        private ConnectionSettings _settings;

        public ObsState State => _state;
        public ConnectionSettings Settings => _settings;

        public ObservableCollection<AudioInputViewModel> AudioMixerItems { get; } = new ObservableCollection<AudioInputViewModel>();
        public ObservableCollection<QuickActionItem> QuickActions { get; } = new ObservableCollection<QuickActionItem>();

        public event Action OpenSettingsRequested;
        public event Action<string> NotificationRequested;
        public event Func<string, string, Task<bool>> ConfirmationRequested;

        // Commands
        public ICommand OpenSettingsCommand { get; }
        public ICommand RetryConnectCommand { get; }

        public ICommand ToggleStreamCommand { get; }
        public ICommand StartStreamCommand { get; }
        public ICommand StopStreamCommand { get; }

        public string StudioModeBadgeText => _state.IsStudioMode ? "Studio mode" : "Direct mode";

        public ICommand ToggleRecordCommand { get; }
        public ICommand StartRecordCommand { get; }
        public ICommand StopRecordCommand { get; }
        public ICommand TogglePauseRecordCommand { get; }

        public ICommand SaveReplayCommand { get; }
        public ICommand ToggleReplayBufferCommand { get; }
        public ICommand StartReplayBufferCommand { get; }
        public ICommand StopReplayBufferCommand { get; }

        public ICommand SwitchSceneCommand { get; }
        public ICommand SwitchPreviewSceneCommand { get; }
        public ICommand TriggerTransitionCommand { get; }
        public ICommand ToggleStudioModeCommand { get; }
        public ICommand ToggleSourceVisibilityCommand { get; }
        public ICommand SetSourceVisibilityCommand { get; }

        public ICommand ToggleMicCommand { get; }
        public ICommand ToggleDesktopCommand { get; }
        public ICommand ToggleVirtualCamCommand { get; }

        public ICommand ExecuteQuickActionCommand { get; }

        public MainWidgetViewModel(
            ObsState state,
            IObsConnectionManager connectionManager,
            ObsStreamService streamService,
            ObsRecordingService recordingService,
            ObsReplayService replayService,
            ObsSceneService sceneService,
            ObsSourceService sourceService,
            ObsAudioService audioService,
            ConnectionSettings settings,
            IObsWebSocketService webSocketService = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _streamService = streamService ?? throw new ArgumentNullException(nameof(streamService));
            _recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
            _replayService = replayService ?? throw new ArgumentNullException(nameof(replayService));
            _sceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
            _sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _settings = settings ?? new ConnectionSettings();
            _webSocketService = webSocketService;

            // Forward notifications
            _streamService.NotificationRequested += msg => NotificationRequested?.Invoke(msg);
            _recordingService.NotificationRequested += msg => NotificationRequested?.Invoke(msg);
            _replayService.NotificationRequested += msg => NotificationRequested?.Invoke(msg);

            // Sync connection manager state with ObsState
            _connectionManager.StateChanged += (s, e) =>
            {
                _state.ConnectionStatus = e.NewStatus;
                _state.StatusMessage = e.Message;
            };

            _state.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ObsState.IsStudioMode))
                {
                    OnPropertyChanged(nameof(StudioModeBadgeText));
                }
            };

            // Audio mixer item mapping
            _state.AudioInputs.CollectionChanged += (s, e) =>
            {
                RunOnUIThread(() =>
                {
                    AudioMixerItems.Clear();
                    foreach (var input in _state.AudioInputs)
                    {
                        AudioMixerItems.Add(new AudioInputViewModel(input, _audioService));
                    }
                });
            };

            // Init Quick Actions (8 slots)
            InitDefaultQuickActions();

            // Commands initialization
            OpenSettingsCommand = new RelayCommand(() => OpenSettingsRequested?.Invoke());
            RetryConnectCommand = new AsyncRelayCommand(async () => await _connectionManager.RetryConnectNowAsync());

            // Stream commands
            StartStreamCommand = new AsyncRelayCommand(async () =>
            {
                if (_settings.ConfirmStream && ConfirmationRequested != null)
                {
                    bool confirmed = await ConfirmationRequested("Start Stream", "Are you sure you want to start streaming?");
                    if (!confirmed) return;
                }
                await _streamService.StartStreamAsync();
            });

            StopStreamCommand = new AsyncRelayCommand(async () =>
            {
                if (ConfirmationRequested != null)
                {
                    bool confirmed = await ConfirmationRequested("Stop Stream", "Are you sure you want to stop streaming?");
                    if (!confirmed) return;
                }
                await _streamService.StopStreamAsync();
            });

            ToggleStreamCommand = new AsyncRelayCommand(async () =>
            {
                if (_state.IsStreaming)
                {
                    if (ConfirmationRequested != null)
                    {
                        bool confirmed = await ConfirmationRequested("Stop Stream", "Are you sure you want to stop streaming?");
                        if (!confirmed) return;
                    }
                    await _streamService.StopStreamAsync();
                }
                else
                {
                    if (_settings.ConfirmStream && ConfirmationRequested != null)
                    {
                        bool confirmed = await ConfirmationRequested("Start Stream", "Are you sure you want to start streaming?");
                        if (!confirmed) return;
                    }
                    await _streamService.StartStreamAsync();
                }
            });

            // Recording commands
            StartRecordCommand = new AsyncRelayCommand(async () => await _recordingService.StartRecordAsync());

            StopRecordCommand = new AsyncRelayCommand(async () =>
            {
                if (_settings.ConfirmRecord && ConfirmationRequested != null)
                {
                    bool confirmed = await ConfirmationRequested("Stop Recording", "Are you sure you want to stop recording?");
                    if (!confirmed) return;
                }
                await _recordingService.StopRecordAsync();
            });

            ToggleRecordCommand = new AsyncRelayCommand(async () =>
            {
                if (_state.IsRecording)
                {
                    if (_settings.ConfirmRecord && ConfirmationRequested != null)
                    {
                        bool confirmed = await ConfirmationRequested("Stop Recording", "Are you sure you want to stop recording?");
                        if (!confirmed) return;
                    }
                    await _recordingService.StopRecordAsync();
                }
                else
                {
                    await _recordingService.StartRecordAsync();
                }
            });

            TogglePauseRecordCommand = new AsyncRelayCommand(async () => await _recordingService.ToggleRecordPauseAsync());

            // Replay buffer commands
            SaveReplayCommand = new AsyncRelayCommand(async () => await _replayService.SaveReplayBufferAsync());
            ToggleReplayBufferCommand = new AsyncRelayCommand(async () => await _replayService.ToggleReplayBufferAsync());
            StartReplayBufferCommand = new AsyncRelayCommand(async () => await _replayService.StartReplayBufferAsync());
            StopReplayBufferCommand = new AsyncRelayCommand(async () => await _replayService.StopReplayBufferAsync());

            // Scene commands
            SwitchSceneCommand = new RelayCommand<SceneModel>(async scene =>
            {
                if (scene == null) return;
                if (_state.IsStudioMode)
                {
                    // In Studio Mode: clicking a scene always stages it into Preview (matching OBS dock behavior)
                    await _sceneService.SetCurrentPreviewSceneAsync(scene.Name).ConfigureAwait(false);
                }
                else
                {
                    // In Normal Mode: switch Program scene directly
                    await _sceneService.SetCurrentProgramSceneAsync(scene.Name).ConfigureAwait(false);
                }
                _ = _audioService?.RefreshAudioInputsAsync(scene.Name);
            });

            SwitchPreviewSceneCommand = new RelayCommand<SceneModel>(async scene =>
            {
                if (scene == null) return;
                await _sceneService.SetCurrentPreviewSceneAsync(scene.Name).ConfigureAwait(false);
                _ = _audioService?.RefreshAudioInputsAsync(scene.Name);
            });

            TriggerTransitionCommand = new AsyncRelayCommand(async () => await _sceneService.TriggerStudioModeTransitionAsync().ConfigureAwait(false));
            ToggleStudioModeCommand = new AsyncRelayCommand(async () => await _sceneService.ToggleStudioModeAsync().ConfigureAwait(false));

            // Source commands
            ToggleSourceVisibilityCommand = new RelayCommand<SourceModel>(async src =>
            {
                if (src == null) return;
                await _sourceService.SetSourceVisibilityAsync(src.SceneName, src.SceneItemId, !src.IsEnabled).ConfigureAwait(false);
            });

            SetSourceVisibilityCommand = new RelayCommand<(SourceModel source, bool isEnabled)>(async param =>
            {
                if (param.source == null) return;
                await _sourceService.SetSourceVisibilityAsync(param.source.SceneName, param.source.SceneItemId, param.isEnabled).ConfigureAwait(false);
            });

            // Quick actions commands
            ToggleMicCommand = new AsyncRelayCommand(async () =>
            {
                var mic = _state.AudioInputs.FirstOrDefault(a => a.InputName.IndexOf("mic", StringComparison.OrdinalIgnoreCase) >= 0)
                          ?? _state.AudioInputs.FirstOrDefault();
                if (mic != null)
                {
                    await _audioService.ToggleInputMuteAsync(mic.InputName);
                }
            });

            ToggleDesktopCommand = new AsyncRelayCommand(async () =>
            {
                var desktop = _state.AudioInputs.FirstOrDefault(a => a.InputName.IndexOf("desktop", StringComparison.OrdinalIgnoreCase) >= 0)
                              ?? _state.AudioInputs.LastOrDefault();
                if (desktop != null)
                {
                    await _audioService.ToggleInputMuteAsync(desktop.InputName);
                }
            });

            ToggleVirtualCamCommand = new AsyncRelayCommand(async () =>
            {
                if (_webSocketService != null)
                {
                    await _webSocketService.SendRequestAsync("ToggleVirtualCam").ConfigureAwait(false);
                }
            });

            // Quick actions
            ExecuteQuickActionCommand = new RelayCommand<QuickActionItem>(async action =>
            {
                if (action == null) return;
                await HandleQuickActionAsync(action);
            });
        }

        private void InitDefaultQuickActions()
        {
            QuickActions.Clear();
            QuickActions.Add(new QuickActionItem(0, QuickActionType.ToggleStream));
            QuickActions.Add(new QuickActionItem(1, QuickActionType.ToggleRecord));
            QuickActions.Add(new QuickActionItem(2, QuickActionType.SaveReplay));
            QuickActions.Add(new QuickActionItem(3, QuickActionType.ToggleReplayBuffer));
            QuickActions.Add(new QuickActionItem(4, QuickActionType.ToggleMic));
            QuickActions.Add(new QuickActionItem(5, QuickActionType.ToggleDesktop));
            QuickActions.Add(new QuickActionItem(6, QuickActionType.TriggerTransition));
            QuickActions.Add(new QuickActionItem(7, QuickActionType.ToggleVirtualCam));
        }

        public async Task HandleQuickActionAsync(QuickActionItem item)
        {
            switch (item.ActionType)
            {
                case QuickActionType.ToggleStream:
                    ToggleStreamCommand.Execute(null);
                    break;
                case QuickActionType.StartStream:
                    StartStreamCommand.Execute(null);
                    break;
                case QuickActionType.StopStream:
                    StopStreamCommand.Execute(null);
                    break;
                case QuickActionType.ToggleRecord:
                    ToggleRecordCommand.Execute(null);
                    break;
                case QuickActionType.StartRecord:
                    StartRecordCommand.Execute(null);
                    break;
                case QuickActionType.StopRecord:
                    StopRecordCommand.Execute(null);
                    break;
                case QuickActionType.ToggleRecordPause:
                    TogglePauseRecordCommand.Execute(null);
                    break;
                case QuickActionType.SaveReplay:
                    SaveReplayCommand.Execute(null);
                    break;
                case QuickActionType.ToggleReplayBuffer:
                    ToggleReplayBufferCommand.Execute(null);
                    break;
                case QuickActionType.ToggleMic:
                    var mic = _state.AudioInputs.FirstOrDefault(a => a.InputName.IndexOf("mic", StringComparison.OrdinalIgnoreCase) >= 0)
                              ?? _state.AudioInputs.FirstOrDefault();
                    if (mic != null)
                    {
                        await _audioService.ToggleInputMuteAsync(mic.InputName);
                    }
                    break;
                case QuickActionType.ToggleDesktop:
                    var desktop = _state.AudioInputs.FirstOrDefault(a => a.InputName.IndexOf("desktop", StringComparison.OrdinalIgnoreCase) >= 0)
                                  ?? _state.AudioInputs.LastOrDefault();
                    if (desktop != null)
                    {
                        await _audioService.ToggleInputMuteAsync(desktop.InputName);
                    }
                    break;
                case QuickActionType.TriggerTransition:
                    TriggerTransitionCommand.Execute(null);
                    break;
                case QuickActionType.ToggleVirtualCam:
                    if (_webSocketService != null)
                    {
                        await _webSocketService.SendRequestAsync("ToggleVirtualCam").ConfigureAwait(false);
                    }
                    break;
                case QuickActionType.ToggleStudioMode:
                    ToggleStudioModeCommand.Execute(null);
                    break;
            }
        }
    }
}
