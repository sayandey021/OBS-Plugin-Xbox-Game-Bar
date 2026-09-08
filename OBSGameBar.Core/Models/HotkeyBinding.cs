using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OBSGameBar.Core.Models
{
    public class HotkeyBinding : INotifyPropertyChanged
    {
        private string _id;
        private string _actionName;
        private string _description;
        private string _key;
        private bool _isCtrl;
        private bool _isAlt;
        private bool _isShift;
        private bool _isEnabled = true;

        public string Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string ActionName
        {
            get => _actionName;
            set => SetField(ref _actionName, value);
        }

        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public string Key
        {
            get => _key;
            set
            {
                if (SetField(ref _key, value))
                {
                    OnPropertyChanged(nameof(DisplayCombination));
                }
            }
        }

        public bool IsCtrl
        {
            get => _isCtrl;
            set
            {
                if (SetField(ref _isCtrl, value))
                {
                    OnPropertyChanged(nameof(DisplayCombination));
                }
            }
        }

        public bool IsAlt
        {
            get => _isAlt;
            set
            {
                if (SetField(ref _isAlt, value))
                {
                    OnPropertyChanged(nameof(DisplayCombination));
                }
            }
        }

        public bool IsShift
        {
            get => _isShift;
            set
            {
                if (SetField(ref _isShift, value))
                {
                    OnPropertyChanged(nameof(DisplayCombination));
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetField(ref _isEnabled, value);
        }

        public string DisplayCombination
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (IsCtrl) parts.Add("Ctrl");
                if (IsAlt) parts.Add("Alt");
                if (IsShift) parts.Add("Shift");
                if (!string.IsNullOrEmpty(Key)) parts.Add(Key);
                return parts.Count > 0 ? string.Join(" + ", parts) : "None";
            }
        }

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

