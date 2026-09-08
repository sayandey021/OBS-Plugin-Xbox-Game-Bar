using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OBSGameBar.Core.Models
{
    public class SourceModel : INotifyPropertyChanged
    {
        private long _sceneItemId;
        private string _sourceName;
        private bool _isEnabled;
        private string _inputKind;
        private string _sceneName;

        public long SceneItemId
        {
            get => _sceneItemId;
            set => SetField(ref _sceneItemId, value);
        }

        public string SourceName
        {
            get => _sourceName;
            set => SetField(ref _sourceName, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        public string InputKind
        {
            get => _inputKind;
            set => SetField(ref _inputKind, value);
        }

        public string SceneName
        {
            get => _sceneName;
            set => SetField(ref _sceneName, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            ViewModels.ViewModelBase.Marshal(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
            return true;
        }
    }
}

