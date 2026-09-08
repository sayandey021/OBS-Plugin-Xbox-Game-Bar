using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OBSGameBar.Core.ViewModels;

namespace OBSGameBar.Core.Models
{
    public class ObsState : ViewModelBase
    {
        private ObsConnectionStatus _connectionStatus = ObsConnectionStatus.Disconnected;
        private string _statusMessage = "Disconnected";
        private bool _isStreaming;
        private TimeSpan _streamDuration = TimeSpan.Zero;
        private double _streamKbitsPerSec;
        private double _streamFps;
        private long _streamDroppedFrames;
        private long _streamTotalFrames;
        private double _streamDroppedFramesPercent;

        private bool _isRecording;
        private bool _isRecordingPaused;
        private TimeSpan _recordingDuration = TimeSpan.Zero;
        private string _recordingTimecode = "00:00:00";

        private bool _isReplayBufferActive;
        private bool _isReplayBufferSaving;
        private int _replayBufferDurationSeconds = 60;
        private string _lastReplaySavedPath;

        private bool _isVirtualCameraActive;
        private bool _isStudioMode;
        private string _currentProgramScene = string.Empty;
        private string _currentPreviewScene = string.Empty;

        public ObsConnectionStatus ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                if (SetField(ref _connectionStatus, value))
                {
                    OnPropertyChanged(nameof(IsConnected));
                    OnPropertyChanged(nameof(IsConnectingOrReconnecting));
                }
            }
        }

        public bool IsConnected => _connectionStatus == ObsConnectionStatus.Connected;
        public bool IsConnectingOrReconnecting => _connectionStatus == ObsConnectionStatus.Connecting || _connectionStatus == ObsConnectionStatus.Reconnecting;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool IsStreaming
        {
            get => _isStreaming;
            set => SetField(ref _isStreaming, value);
        }

        public TimeSpan StreamDuration
        {
            get => _streamDuration;
            set
            {
                if (SetField(ref _streamDuration, value))
                {
                    OnPropertyChanged(nameof(StreamTimecode));
                }
            }
        }

        public string StreamTimecode => StreamDuration.ToString(@"hh\:mm\:ss");

        public double StreamKbitsPerSec
        {
            get => _streamKbitsPerSec;
            set
            {
                if (SetField(ref _streamKbitsPerSec, value))
                {
                    OnPropertyChanged(nameof(StreamBitrateDisplay));
                }
            }
        }

        public string StreamBitrateDisplay => $"{StreamKbitsPerSec / 1000.0:F1} Mbps";

        public double StreamFps
        {
            get => _streamFps;
            set => SetField(ref _streamFps, value);
        }

        public long StreamDroppedFrames
        {
            get => _streamDroppedFrames;
            set => SetField(ref _streamDroppedFrames, value);
        }

        public long StreamTotalFrames
        {
            get => _streamTotalFrames;
            set => SetField(ref _streamTotalFrames, value);
        }

        public double StreamDroppedFramesPercent
        {
            get => _streamDroppedFramesPercent;
            set
            {
                if (SetField(ref _streamDroppedFramesPercent, value))
                {
                    OnPropertyChanged(nameof(StreamDroppedFramesPercentDisplay));
                }
            }
        }

        public string StreamDroppedFramesPercentDisplay => $"{StreamDroppedFramesPercent:F2}% dropped";

        public bool IsRecording
        {
            get => _isRecording;
            set => SetField(ref _isRecording, value);
        }

        public bool IsRecordingPaused
        {
            get => _isRecordingPaused;
            set => SetField(ref _isRecordingPaused, value);
        }

        public TimeSpan RecordingDuration
        {
            get => _recordingDuration;
            set
            {
                if (SetField(ref _recordingDuration, value))
                {
                    OnPropertyChanged(nameof(RecordingTimecode));
                }
            }
        }

        public string RecordingTimecode
        {
            get => string.IsNullOrEmpty(_recordingTimecode) ? RecordingDuration.ToString(@"hh\:mm\:ss") : _recordingTimecode;
            set => SetField(ref _recordingTimecode, value);
        }

        public bool IsReplayBufferActive
        {
            get => _isReplayBufferActive;
            set => SetField(ref _isReplayBufferActive, value);
        }

        public bool IsReplayBufferSaving
        {
            get => _isReplayBufferSaving;
            set => SetField(ref _isReplayBufferSaving, value);
        }

        public int ReplayBufferDurationSeconds
        {
            get => _replayBufferDurationSeconds;
            set => SetField(ref _replayBufferDurationSeconds, value);
        }

        public string LastReplaySavedPath
        {
            get => _lastReplaySavedPath;
            set => SetField(ref _lastReplaySavedPath, value);
        }

        public bool IsVirtualCameraActive
        {
            get => _isVirtualCameraActive;
            set => SetField(ref _isVirtualCameraActive, value);
        }

        public bool IsStudioMode
        {
            get => _isStudioMode;
            set
            {
                if (SetField(ref _isStudioMode, value))
                {
                    OnPropertyChanged(nameof(SceneSourcesHeader));
                    RunOnUIThread(() =>
                    {
                        foreach (var scene in Scenes)
                        {
                            scene.IsStudioMode = value;
                        }
                    });
                }
            }
        }

        public string SceneSourcesHeader => IsStudioMode ? "Scene sources (Preview)" : "Scene sources";

        public string CurrentProgramScene
        {
            get => _currentProgramScene;
            set => SetField(ref _currentProgramScene, value);
        }

        public string CurrentPreviewScene
        {
            get => _currentPreviewScene;
            set => SetField(ref _currentPreviewScene, value);
        }

        public ObservableCollection<SceneModel> Scenes { get; } = new ObservableCollection<SceneModel>();
        public ObservableCollection<SourceModel> CurrentSceneSources { get; } = new ObservableCollection<SourceModel>();
        public ObservableCollection<AudioInputModel> AudioInputs { get; } = new ObservableCollection<AudioInputModel>();

        public void Reset()
        {
            ConnectionStatus = ObsConnectionStatus.Disconnected;
            StatusMessage = "Disconnected";
            IsStreaming = false;
            StreamDuration = TimeSpan.Zero;
            StreamKbitsPerSec = 0;
            StreamFps = 0;
            StreamDroppedFrames = 0;
            StreamTotalFrames = 0;
            StreamDroppedFramesPercent = 0;

            IsRecording = false;
            IsRecordingPaused = false;
            RecordingDuration = TimeSpan.Zero;
            RecordingTimecode = "00:00:00";

            IsReplayBufferActive = false;
            IsReplayBufferSaving = false;

            IsVirtualCameraActive = false;
            IsStudioMode = false;
            CurrentProgramScene = string.Empty;
            CurrentPreviewScene = string.Empty;

            RunOnUIThread(() =>
            {
                Scenes.Clear();
                CurrentSceneSources.Clear();
                AudioInputs.Clear();
            });
        }

        public void UpdateScenes(IEnumerable<SceneModel> newScenes)
        {
            RunOnUIThread(() =>
            {
                Scenes.Clear();
                if (newScenes != null)
                {
                    foreach (var scene in newScenes)
                    {
                        scene.IsStudioMode = _isStudioMode;
                        Scenes.Add(scene);
                    }
                }
            });
        }

        public void UpdateCurrentSceneSources(IEnumerable<SourceModel> newSources)
        {
            RunOnUIThread(() =>
            {
                CurrentSceneSources.Clear();
                if (newSources != null)
                {
                    foreach (var source in newSources)
                    {
                        CurrentSceneSources.Add(source);
                    }
                }
            });
        }

        public void UpdateAudioInputs(IEnumerable<AudioInputModel> newInputs)
        {
            RunOnUIThread(() =>
            {
                AudioInputs.Clear();
                if (newInputs != null)
                {
                    foreach (var input in newInputs)
                    {
                        AudioInputs.Add(input);
                    }
                }
            });
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            return SetProperty(ref field, value, propertyName);
        }
    }
}
