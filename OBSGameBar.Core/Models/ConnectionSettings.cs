using System;
using OBSGameBar.Core.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OBSGameBar.Core.Models
{
    public class ConnectionSettings : INotifyPropertyChanged
    {
        private string _host = "127.0.0.1";
        private int _port = 4455;
        private string _password = string.Empty;
        private bool _autoConnect = true;
        private bool _confirmStream = false;
        private bool _confirmRecord = false;
        private bool _savePassword = true;

        public string Host
        {
            get => _host;
            set
            {
                if (SetField(ref _host, value))
                {
                    OnPropertyChanged(nameof(IsLocalHost));
                }
            }
        }

        public int Port
        {
            get => _port;
            set => SetField(ref _port, value);
        }

        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }

        public bool AutoConnect
        {
            get => _autoConnect;
            set => SetField(ref _autoConnect, value);
        }

        public bool ConfirmStream
        {
            get => _confirmStream;
            set => SetField(ref _confirmStream, value);
        }

        public bool ConfirmRecord
        {
            get => _confirmRecord;
            set => SetField(ref _confirmRecord, value);
        }

        public bool SavePassword
        {
            get => _savePassword;
            set => SetField(ref _savePassword, value);
        }

        public bool IsLocalHost
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_host)) return true;
                var trimmed = _host.Trim().ToLowerInvariant();
                return trimmed == "127.0.0.1" || trimmed == "localhost" || trimmed == "::1";
            }
        }

        public string WebSocketUri => $"ws://{Host.Trim()}:{Port}";

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

