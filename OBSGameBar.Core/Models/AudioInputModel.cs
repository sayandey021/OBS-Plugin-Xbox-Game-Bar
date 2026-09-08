using System;
using OBSGameBar.Core.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OBSGameBar.Core.Models
{
    public class AudioInputModel : INotifyPropertyChanged
    {
        private string _inputName;
        private string _inputKind;
        private float _volumeDb;
        private float _volumeMul;
        private bool _isMuted;
        private float _peakDb = -100f;

        public string InputName
        {
            get => _inputName;
            set => SetField(ref _inputName, value);
        }

        public string InputKind
        {
            get => _inputKind;
            set => SetField(ref _inputKind, value);
        }

        public float VolumeDb
        {
            get => _volumeDb;
            set
            {
                if (SetField(ref _volumeDb, value))
                {
                    OnPropertyChanged(nameof(VolumeDisplayString));
                }
            }
        }

        public float VolumeMul
        {
            get => _volumeMul;
            set => SetField(ref _volumeMul, value);
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (SetField(ref _isMuted, value))
                {
                    OnPropertyChanged(nameof(MuteIconGlyph));
                }
            }
        }

        public float PeakDb
        {
            get => _peakDb;
            set => SetField(ref _peakDb, value);
        }

        public string VolumeDisplayString
        {
            get
            {
                if (_volumeDb <= -100f || float.IsNegativeInfinity(_volumeDb))
                    return "-∞ dB";
                return $"{_volumeDb:F1} dB";
            }
        }

        public string MuteIconGlyph => _isMuted ? "\uE74F" : "\uE767"; // Volume mute vs Volume

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            ViewModels.ViewModelBase.Marshal(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ViewModels.ViewModelBase.Marshal(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
    }
}

