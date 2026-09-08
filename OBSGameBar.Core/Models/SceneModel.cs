using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OBSGameBar.Core.Models
{
    public class SceneModel : INotifyPropertyChanged
    {
        private string _name;
        private int _index;
        private bool _isActive;
        private bool _isPreview;
        private bool _isStudioMode;

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public int Index
        {
            get => _index;
            set => SetField(ref _index, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetField(ref _isActive, value))
                {
                    OnPropertyChanged(nameof(ShowLiveBadge));
                    OnPropertyChanged(nameof(ShowPreviewBadge));
                    OnPropertyChanged(nameof(StatusBadgeText));
                    OnPropertyChanged(nameof(HasStatusBadge));
                    OnPropertyChanged(nameof(SceneState));
                }
            }
        }

        public bool IsPreview
        {
            get => _isPreview;
            set
            {
                if (SetField(ref _isPreview, value))
                {
                    OnPropertyChanged(nameof(ShowLiveBadge));
                    OnPropertyChanged(nameof(ShowPreviewBadge));
                    OnPropertyChanged(nameof(StatusBadgeText));
                    OnPropertyChanged(nameof(HasStatusBadge));
                    OnPropertyChanged(nameof(SceneState));
                }
            }
        }

        public bool IsStudioMode
        {
            get => _isStudioMode;
            set
            {
                if (SetField(ref _isStudioMode, value))
                {
                    OnPropertyChanged(nameof(ShowLiveBadge));
                    OnPropertyChanged(nameof(ShowPreviewBadge));
                    OnPropertyChanged(nameof(StatusBadgeText));
                    OnPropertyChanged(nameof(HasStatusBadge));
                    OnPropertyChanged(nameof(SceneState));
                }
            }
        }

        public bool ShowLiveBadge => IsStudioMode && IsActive;
        public bool ShowPreviewBadge => IsStudioMode && IsPreview;

        public string SceneState
        {
            get
            {
                if (!IsStudioMode)
                {
                    return IsActive ? "NormalActive" : "Inactive";
                }
                if (IsActive && IsPreview) return "StudioLivePreview";
                if (IsActive) return "StudioLive";
                if (IsPreview) return "StudioPreview";
                return "Inactive";
            }
        }

        public string StatusBadgeText
        {
            get
            {
                if (!IsStudioMode) return string.Empty;
                if (IsActive && IsPreview) return "Live / Preview";
                if (IsActive) return "Live";
                if (IsPreview) return "Preview";
                return string.Empty;
            }
        }

        public bool HasStatusBadge => IsStudioMode && (IsActive || IsPreview);

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ViewModels.ViewModelBase.Marshal(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            ViewModels.ViewModelBase.Marshal(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            return true;
        }
    }
}

