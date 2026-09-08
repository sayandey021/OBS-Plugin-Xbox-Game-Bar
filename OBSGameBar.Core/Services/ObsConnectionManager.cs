using System;
using System.Threading;
using System.Threading.Tasks;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Core.Services
{
    public class ObsConnectionManager : IObsConnectionManager
    {
        private readonly IObsWebSocketService _webSocketService;
        private readonly Func<Task> _synchronizeCallback;
        private ConnectionSettings _settings;
        private CancellationTokenSource _reconnectCts;
        private Task _reconnectTask;
        private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);

        private bool _isAutoConnectRunning;
        private bool _isPaused;
        private int _backoffIndex = 0;
        private static readonly int[] BackoffDelaysSeconds = new[] { 1, 2, 4, 8, 16 };

        public event EventHandler<ObsConnectionStateChangedEventArgs> StateChanged;

        public ObsConnectionStatus Status => _webSocketService.Status;
        public bool IsConnected => _webSocketService.IsConnected;
        public string StatusMessage { get; private set; } = "Disconnected";

        public ObsConnectionManager(IObsWebSocketService webSocketService, Func<Task> synchronizeCallback = null)
        {
            _webSocketService = webSocketService ?? throw new ArgumentNullException(nameof(webSocketService));
            _synchronizeCallback = synchronizeCallback;

            _webSocketService.StateChanged += OnWebSocketStateChanged;
        }

        public void Configure(ConnectionSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task StartAutoConnectAsync()
        {
            if (_isAutoConnectRunning) return;
            _isAutoConnectRunning = true;
            _backoffIndex = 0;

            await TryConnectAsync().ConfigureAwait(false);
        }

        public async Task StopAutoConnectAsync()
        {
            _isAutoConnectRunning = false;
            CancelReconnectLoop();
            await _webSocketService.DisconnectAsync().ConfigureAwait(false);
        }

        public async Task RetryConnectNowAsync()
        {
            CancelReconnectLoop();
            _backoffIndex = 0;
            await TryConnectAsync().ConfigureAwait(false);
        }

        public void Pause()
        {
            _isPaused = true;
            CancelReconnectLoop();
        }

        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused = false;

            if (_isAutoConnectRunning && !IsConnected)
            {
                _backoffIndex = 0;
                _ = TryConnectAsync();
            }
        }

        private async Task TryConnectAsync()
        {
            if (_settings == null || _isPaused) return;

            bool lockTaken = await _connectLock.WaitAsync(0).ConfigureAwait(false);
            if (!lockTaken) return;

            try
            {
                CancelReconnectLoop();
                StatusMessage = "Connecting to OBS...";
                RaiseStateChanged(ObsConnectionStatus.Connecting, StatusMessage);

                await _webSocketService.ConnectAsync(_settings.Host, _settings.Port, _settings.Password).ConfigureAwait(false);

                // Connected successfully!
                _backoffIndex = 0;
                StatusMessage = "Connected to OBS";
                RaiseStateChanged(ObsConnectionStatus.Connected, StatusMessage);

                if (_synchronizeCallback != null)
                {
                    try
                    {
                        await _synchronizeCallback().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ObsConnectionManager] State sync error: {ex.Message}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                StatusMessage = "Authentication failed. Check your OBS WebSocket password.";
                RaiseStateChanged(ObsConnectionStatus.AuthFailed, StatusMessage);
                // Do not immediately retry in an infinite loop on bad password
            }
            catch (Exception)
            {
                StatusMessage = $"Unable to connect to OBS at {_settings.Host}:{_settings.Port}";
                RaiseStateChanged(ObsConnectionStatus.Disconnected, StatusMessage);

                if (_isAutoConnectRunning && !_isPaused)
                {
                    ScheduleReconnect();
                }
            }
            finally
            {
                _connectLock.Release();
            }
        }

        private void ScheduleReconnect()
        {
            if (_isPaused || !_isAutoConnectRunning) return;

            CancelReconnectLoop();
            _reconnectCts = new CancellationTokenSource();
            var token = _reconnectCts.Token;

            int delaySeconds = BackoffDelaysSeconds[Math.Min(_backoffIndex, BackoffDelaysSeconds.Length - 1)];
            _backoffIndex++;

            StatusMessage = $"Reconnecting in {delaySeconds}s...";
            RaiseStateChanged(ObsConnectionStatus.Reconnecting, StatusMessage);

            _reconnectTask = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token).ConfigureAwait(false);
                    if (!token.IsCancellationRequested)
                    {
                        await TryConnectAsync().ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
            }, token);
        }

        private void OnWebSocketStateChanged(object sender, ObsConnectionStateChangedEventArgs e)
        {
            if (e.NewStatus == ObsConnectionStatus.Disconnected && _isAutoConnectRunning && !_isPaused)
            {
                StatusMessage = "OBS disconnected. Retrying...";
                ScheduleReconnect();
            }
            else if (e.NewStatus == ObsConnectionStatus.AuthFailed)
            {
                StatusMessage = "Authentication failed. Check your OBS WebSocket password.";
                RaiseStateChanged(ObsConnectionStatus.AuthFailed, StatusMessage);
            }
        }

        private void CancelReconnectLoop()
        {
            if (_reconnectCts != null)
            {
                try
                {
                    _reconnectCts.Cancel();
                    _reconnectCts.Dispose();
                }
                catch { }
                _reconnectCts = null;
            }
        }

        private void RaiseStateChanged(ObsConnectionStatus status, string message)
        {
            StateChanged?.Invoke(this, new ObsConnectionStateChangedEventArgs(Status, status, message));
        }

        public void Dispose()
        {
            _webSocketService.StateChanged -= OnWebSocketStateChanged;
            CancelReconnectLoop();
            _connectLock.Dispose();
        }
    }
}
