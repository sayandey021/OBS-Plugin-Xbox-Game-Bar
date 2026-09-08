using System;
using System.Windows.Input;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Services;

namespace OBSGameBar.Core.ViewModels
{
    public class AudioInputViewModel : ViewModelBase
    {
        private readonly AudioInputModel _model;
        private readonly ObsAudioService _audioService;

        public AudioInputModel Model => _model;

        public string InputName => _model.InputName;
        public string InputKind => _model.InputKind;

        public float VolumeDb
        {
            get => _model.VolumeDb;
            set
            {
                if (Math.Abs(_model.VolumeDb - value) > 0.1f)
                {
                    _model.VolumeDb = value;
                    _audioService?.SetVolumeDbDebounced(_model.InputName, value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VolumeDisplayString));
                }
            }
        }

        public bool IsMuted
        {
            get => _model.IsMuted;
            set
            {
                if (_model.IsMuted != value)
                {
                    _model.IsMuted = value;
                    _ = _audioService?.SetInputMuteAsync(_model.InputName, value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(MuteIconGlyph));
                }
            }
        }

        public string VolumeDisplayString => _model.VolumeDisplayString;
        public string MuteIconGlyph => _model.MuteIconGlyph;

        public ICommand ToggleMuteCommand { get; }

        public AudioInputViewModel(AudioInputModel model, ObsAudioService audioService)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));

            _model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AudioInputModel.VolumeDb))
                {
                    OnPropertyChanged(nameof(VolumeDb));
                    OnPropertyChanged(nameof(VolumeDisplayString));
                }
                else if (e.PropertyName == nameof(AudioInputModel.IsMuted))
                {
                    OnPropertyChanged(nameof(IsMuted));
                    OnPropertyChanged(nameof(MuteIconGlyph));
                }
            };

            ToggleMuteCommand = new AsyncRelayCommand(async () =>
            {
                await _audioService.ToggleInputMuteAsync(_model.InputName).ConfigureAwait(false);
            });
        }
    }
}
