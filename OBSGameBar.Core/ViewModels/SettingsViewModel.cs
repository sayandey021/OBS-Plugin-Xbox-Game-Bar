using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Services;

namespace OBSGameBar.Core.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ConnectionSettings _settings;
        private readonly IObsConnectionManager _connectionManager;
        private readonly ISecureStorageService _secureStorage;

        private string _host;
        private int _port;
        private string _password;
        private bool _autoConnect;
        private bool _confirmStream;
        private bool _confirmRecord;

        private string _testStatusMessage;
        private bool _isTesting;
        private bool _testSuccessful;
        private bool _savedNotificationVisible;

        public string Host
        {
            get => _host;
            set
            {
                if (SetProperty(ref _host, value))
                {
                    OnPropertyChanged(nameof(ShowNonLocalWarning));
                }
            }
        }

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool AutoConnect
        {
            get => _autoConnect;
            set => SetProperty(ref _autoConnect, value);
        }

        public bool ConfirmStream
        {
            get => _confirmStream;
            set => SetProperty(ref _confirmStream, value);
        }

        public bool ConfirmRecord
        {
            get => _confirmRecord;
            set => SetProperty(ref _confirmRecord, value);
        }

        public bool ShowNonLocalWarning
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_host)) return false;
                var trimmed = _host.Trim().ToLowerInvariant();
                return trimmed != "127.0.0.1" && trimmed != "localhost" && trimmed != "::1";
            }
        }

        public string TestStatusMessage
        {
            get => _testStatusMessage;
            set => SetProperty(ref _testStatusMessage, value);
        }

        public bool IsTesting
        {
            get => _isTesting;
            set => SetProperty(ref _isTesting, value);
        }

        public bool TestSuccessful
        {
            get => _testSuccessful;
            set => SetProperty(ref _testSuccessful, value);
        }

        public bool SavedNotificationVisible
        {
            get => _savedNotificationVisible;
            set => SetProperty(ref _savedNotificationVisible, value);
        }

        public ObservableCollection<HotkeyBinding> Hotkeys { get; } = new ObservableCollection<HotkeyBinding>();

        public ICommand TestConnectionCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public event Action SettingsSaved;

        public SettingsViewModel(
            ConnectionSettings settings,
            IObsConnectionManager connectionManager,
            ISecureStorageService secureStorage)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));

            _host = _settings.Host;
            _port = _settings.Port;
            _password = _settings.Password;
            _autoConnect = _settings.AutoConnect;
            _confirmStream = _settings.ConfirmStream;
            _confirmRecord = _settings.ConfirmRecord;

            InitDefaultHotkeys();

            TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            ResetDefaultsCommand = new RelayCommand(ResetDefaults);
        }

        private void InitDefaultHotkeys()
        {
            Hotkeys.Clear();
            Hotkeys.Add(new HotkeyBinding
            {
                Id = "rec",
                ActionName = "Toggle Recording",
                Description = "Start or stop recording",
                Key = "R",
                IsCtrl = true,
                IsAlt = true
            });
            Hotkeys.Add(new HotkeyBinding
            {
                Id = "stream",
                ActionName = "Toggle Stream",
                Description = "Start or stop streaming",
                Key = "S",
                IsCtrl = true,
                IsAlt = true
            });
            Hotkeys.Add(new HotkeyBinding
            {
                Id = "replay",
                ActionName = "Save Replay",
                Description = "Save instant replay to disk",
                Key = "C",
                IsCtrl = true,
                IsAlt = true
            });
            Hotkeys.Add(new HotkeyBinding
            {
                Id = "mic",
                ActionName = "Toggle Mic Mute",
                Description = "Mute or unmute microphone",
                Key = "M",
                IsCtrl = true,
                IsAlt = true
            });
            Hotkeys.Add(new HotkeyBinding
            {
                Id = "replay_buf",
                ActionName = "Toggle Replay Buffer",
                Description = "Start or stop replay buffer",
                Key = "B",
                IsCtrl = true,
                IsAlt = true
            });
        }

        private async Task TestConnectionAsync()
        {
            IsTesting = true;
            TestStatusMessage = "Testing connection to OBS...";
            TestSuccessful = false;

            using (var testService = new ObsWebSocketService())
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                try
                {
                    await testService.ConnectAsync(Host, Port, Password, cts.Token).ConfigureAwait(false);
                    TestSuccessful = true;
                    TestStatusMessage = "Connection successful! OBS is responding.";
                    await testService.DisconnectAsync().ConfigureAwait(false);
                }
                catch (UnauthorizedAccessException)
                {
                    TestSuccessful = false;
                    TestStatusMessage = "Authentication failed. Check your OBS WebSocket password.";
                }
                catch (Exception ex)
                {
                    TestSuccessful = false;
                    TestStatusMessage = $"Unable to connect: {ex.Message}";
                }
                finally
                {
                    IsTesting = false;
                }
            }
        }

        private async Task SaveSettingsAsync()
        {
            _settings.Host = Host?.Trim() ?? "127.0.0.1";
            _settings.Port = Port;
            _settings.Password = Password ?? string.Empty;
            _settings.AutoConnect = AutoConnect;
            _settings.ConfirmStream = ConfirmStream;
            _settings.ConfirmRecord = ConfirmRecord;

            // Securely persist password
            if (_secureStorage != null)
            {
                try
                {
                    await _secureStorage.SavePasswordAsync("OBSGameBar", "WebSocket", _settings.Password);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Secure save error: {ex.Message}");
                }
            }

            _connectionManager.Configure(_settings);
            _ = _connectionManager.RetryConnectNowAsync();

            SavedNotificationVisible = true;
            SettingsSaved?.Invoke();

            _ = Task.Delay(2500).ContinueWith(_ => SavedNotificationVisible = false);
        }

        private void ResetDefaults()
        {
            Host = "127.0.0.1";
            Port = 4455;
            Password = string.Empty;
            AutoConnect = true;
            ConfirmStream = false;
            ConfirmRecord = false;
            InitDefaultHotkeys();
        }
    }
}
