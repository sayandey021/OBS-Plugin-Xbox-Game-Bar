using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;
using OBSGameBar.Core.Models;
using OBSGameBar.Core.Services;
using OBSGameBar.Core.ViewModels;
using OBSGameBar.Services;
using OBSGameBar.Views;

namespace OBSGameBar
{
    sealed partial class App : Application
    {
        private XboxGameBarWidget _mainWidget;
        private XboxGameBarWidget _settingsWidget;
        private static readonly List<CoreDispatcher> _dispatchers = new List<CoreDispatcher>();
        private static bool _autoConnectStarted;

        public static ConnectionSettings Settings { get; private set; }
        public static ISecureStorageService SecureStorage { get; private set; }
        public static ObsState ObsState { get; private set; }
        public static IObsWebSocketService WebSocketService { get; private set; }
        public static IObsConnectionManager ConnectionManager { get; private set; }

        public static ObsStreamService StreamService { get; private set; }
        public static ObsRecordingService RecordingService { get; private set; }
        public static ObsReplayService ReplayService { get; private set; }
        public static ObsSceneService SceneService { get; private set; }
        public static ObsSourceService SourceService { get; private set; }
        public static ObsAudioService AudioService { get; private set; }

        public static MainWidgetViewModel MainViewModel { get; private set; }
        public static SettingsViewModel SettingsViewModel { get; private set; }

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Log($"AppDomain Unhandled: {e.ExceptionObject}");
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Log($"UnobservedTaskException: {e.Exception}");
                e.SetObserved();
            };

            ViewModelBase.Logger = Log;
            Log("App constructor started");
            try
            {
                this.InitializeComponent();
                this.UnhandledException += OnUnhandledException;
                InitializeObsServices();
                Log("App constructor completed");
            }
            catch (Exception ex)
            {
                Log($"App constructor exception: {ex}");
            }
        }

        public static void Log(string message)
        {
            Program.WriteLog(message);
            string entry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            try
            {
                System.Diagnostics.Debug.WriteLine($"[OBSGameBar] {entry}");
                var localSettings = ApplicationData.Current?.LocalSettings;
                if (localSettings != null)
                {
                    string old = localSettings.Values.ContainsKey("debug_log") ? (localSettings.Values["debug_log"] as string ?? "") : "";
                    if (old.Length > 16000) old = old.Substring(old.Length - 8000);
                    localSettings.Values["debug_log"] = old + entry + "\n";
                }
            }
            catch { }

            try
            {
                string folder = ApplicationData.Current?.LocalFolder?.Path;
                if (!string.IsNullOrEmpty(folder))
                {
                    string file = Path.Combine(folder, "app_debug.log");
                    File.AppendAllText(file, entry + "\r\n");
                }
            }
            catch { }
        }

        private void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Log($"Unhandled Exception: {e.Message} | {e.Exception?.ToString()} | HResult: 0x{e.Exception?.HResult:X8}");
            if (e.Exception?.InnerException != null)
            {
                Log($"Inner Exception: {e.Exception.InnerException}");
            }
            e.Handled = true;
        }

        private void InitializeObsServices()
        {
            Log("InitializeObsServices started");
            try
            {
                Settings = new ConnectionSettings();
                SecureStorage = new UwpPasswordVaultStorage();
                ObsState = new ObsState();
                WebSocketService = new ObsWebSocketService();

                Func<Task> synchronizeCallback = async () =>
                {
                    Log("SynchronizeCallback invoked");
                    try { Log("Sync 1: StreamService"); await StreamService.RefreshStatusAsync().ConfigureAwait(false); } catch (Exception ex) { Log($"Sync 1 error: {ex}"); }
                    try { Log("Sync 2: RecordingService"); await RecordingService.RefreshStatusAsync().ConfigureAwait(false); } catch (Exception ex) { Log($"Sync 2 error: {ex}"); }
                    try { Log("Sync 3: ReplayService"); await ReplayService.RefreshStatusAsync().ConfigureAwait(false); } catch (Exception ex) { Log($"Sync 3 error: {ex}"); }
                    try { Log("Sync 4: SceneService"); await SceneService.RefreshScenesAsync().ConfigureAwait(false); } catch (Exception ex) { Log($"Sync 4 error: {ex}"); }
                    try { Log("Sync 5: AudioService"); await AudioService.RefreshAudioInputsAsync().ConfigureAwait(false); } catch (Exception ex) { Log($"Sync 5 error: {ex}"); }
                    Log("SynchronizeCallback finished");
                };

                ConnectionManager = new ObsConnectionManager(WebSocketService, synchronizeCallback);
                ConnectionManager.Configure(Settings);

                StreamService = new ObsStreamService(WebSocketService, ObsState);
                RecordingService = new ObsRecordingService(WebSocketService, ObsState);
                ReplayService = new ObsReplayService(WebSocketService, ObsState);

                SceneService = new ObsSceneService(WebSocketService, ObsState, activeScene =>
                {
                    _ = SourceService?.RefreshSourcesForCurrentSceneAsync(activeScene);
                    _ = AudioService?.RefreshAudioInputsAsync(activeScene);
                });

                SourceService = new ObsSourceService(WebSocketService, ObsState);
                AudioService = new ObsAudioService(WebSocketService, ObsState);

                MainViewModel = new MainWidgetViewModel(
                    ObsState,
                    ConnectionManager,
                    StreamService,
                    RecordingService,
                    ReplayService,
                    SceneService,
                    SourceService,
                    AudioService,
                    Settings,
                    WebSocketService);

                SettingsViewModel = new SettingsViewModel(
                    Settings,
                    ConnectionManager,
                    SecureStorage);

                MainViewModel.NotificationRequested += msg =>
                {
                    GameBarNotificationService.ShowNotification("OBS Studio", msg);
                };
                Log("InitializeObsServices finished");
            }
            catch (Exception ex)
            {
                Log($"InitializeObsServices exception: {ex}");
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            Log($"OnActivated entered. Kind: {args.Kind}");
            try
            {
                XboxGameBarWidgetActivatedEventArgs widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                if (widgetArgs == null && args.Kind == ActivationKind.Protocol)
                {
                    var protocolArgs = args as IProtocolActivatedEventArgs;
                    string scheme = protocolArgs?.Uri?.Scheme;
                    Log($"Protocol scheme: {scheme}, Uri: {protocolArgs?.Uri}");
                    if (scheme != null && scheme.Equals("ms-gamebarwidget", StringComparison.OrdinalIgnoreCase))
                    {
                        widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                    }
                }

                if (widgetArgs != null)
                {
                    Log($"WidgetArgs received. AppExtensionId: {widgetArgs.AppExtensionId}, IsLaunch: {widgetArgs.IsLaunchActivation}");
                    if (widgetArgs.IsLaunchActivation)
                    {
                        var rootFrame = new Frame();
                        rootFrame.NavigationFailed += OnNavigationFailed;
                        Window.Current.Content = rootFrame;
                        if (widgetArgs.AppExtensionId == "OBSControlWidget")
                        {
                            SetDispatcher(Window.Current.CoreWindow.Dispatcher, isPrimary: true);
                            _mainWidget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, rootFrame);
                            rootFrame.Navigate(typeof(MainWidgetView), _mainWidget);

                            _mainWidget.SettingsClicked += async (s, e) =>
                            {
                                try
                                {
                                    await _mainWidget.ActivateSettingsAsync();
                                }
                                catch (Exception ex)
                                {
                                    Log($"ActivateSettings error: {ex.Message}");
                                }
                            };

                            _mainWidget.VisibleChanged += (s, e) =>
                            {
                                if (_mainWidget.Visible) ConnectionManager?.Resume();
                                else ConnectionManager?.Pause();
                            };

                            MainViewModel.OpenSettingsRequested += async () =>
                            {
                                try { await _mainWidget.ActivateSettingsAsync(); }
                                catch { rootFrame.Navigate(typeof(SettingsView)); }
                            };
                        }
                        else if (widgetArgs.AppExtensionId == "OBSControlSettings")
                        {
                            _settingsWidget = new XboxGameBarWidget(widgetArgs, Window.Current.CoreWindow, rootFrame);
                            rootFrame.Navigate(typeof(SettingsView), _settingsWidget);
                        }

                        Window.Current.Activate();
                        StartBackgroundConnection();
                    }
                    else
                    {
                        Window.Current.Activate();
                    }
                }
                else
                {
                    Log($"Non-widget activation received: {args.Kind}");
                    // Same guard as OnLaunched - never build a duplicate MainWidgetView.
                    if (IsUiInitialized())
                    {
                        Log("Non-widget activation: UI already initialized - skipping duplicate view.");
                        Window.Current.Activate();
                        return;
                    }

                    var rootFrame = Window.Current.Content as Frame;
                    if (rootFrame == null)
                    {
                        rootFrame = new Frame();
                        rootFrame.NavigationFailed += OnNavigationFailed;
                        Window.Current.Content = rootFrame;
                    }
                    SetDispatcher(Window.Current.CoreWindow.Dispatcher);
                    if (rootFrame.Content == null)
                    {
                        rootFrame.Navigate(typeof(MainWidgetView));
                    }
                    Window.Current.Activate();
                    StartBackgroundConnection();
                }
            }
            catch (Exception ex)
            {
                Log($"OnActivated exception: {ex}");
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Log("OnLaunched entered");
            try
            {
                // A standalone launch may arrive in the SAME process as an already-
                // open Game Bar widget window. Building a second MainWidgetView would
                // drive its bindings from the wrong thread (RPC_E_WRONG_THREAD) and
                // ruin the widget UI. Only build the main view once - on the first
                // window to register.
                if (IsUiInitialized())
                {
                    Log("OnLaunched: UI already initialized - skipping duplicate view.");
                    Window.Current.Activate();
                    StartBackgroundConnection();
                    return;
                }

                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    Window.Current.Content = rootFrame;
                }

                SetDispatcher(Window.Current.CoreWindow.Dispatcher);

                MainViewModel.OpenSettingsRequested += () =>
                {
                    rootFrame.Navigate(typeof(SettingsView));
                };

                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainWidgetView), e.Arguments);
                }

                Window.Current.Activate();
                StartBackgroundConnection();
                Log("OnLaunched completed");
            }
            catch (Exception ex)
            {
                Log($"OnLaunched exception: {ex}");
            }
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Log($"[App] Navigation failed: {e.Exception?.Message}");
        }

        private static bool IsUiInitialized()
        {
            lock (_dispatchers)
            {
                return _dispatchers.Count > 0;
            }
        }

        /// <summary>
        /// Registers a UI dispatcher. The primary widget dispatcher takes priority
        /// for ViewModel and ObsState property notifications.
        /// </summary>
        public static void SetDispatcher(CoreDispatcher dispatcher, bool isPrimary = false)
        {
            if (dispatcher == null) return;
            lock (_dispatchers)
            {
                if (isPrimary)
                {
                    _dispatchers.Remove(dispatcher);
                    _dispatchers.Insert(0, dispatcher);
                    Log("SetDispatcher: Primary widget dispatcher registered.");
                }
                else
                {
                    if (!_dispatchers.Contains(dispatcher))
                    {
                        _dispatchers.Add(dispatcher);
                        Log("SetDispatcher: Secondary dispatcher registered.");
                    }
                }
            }

            ViewModelBase.DispatcherRunner = action =>
            {
                CoreDispatcher d;
                lock (_dispatchers)
                {
                    d = _dispatchers.Count > 0 ? _dispatchers[0] : null;
                }

                if (d == null)
                {
                    try { action(); } catch { }
                    return;
                }

                try
                {
                    if (d.HasThreadAccess)
                    {
                        action();
                    }
                    else
                    {
                        _ = d.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            try
                            {
                                action();
                            }
                            catch (Exception ex)
                            {
                                Log($"Dispatcher action exception: {ex}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log($"Dispatcher invocation exception: {ex}");
                }
            };
        }

        private static void StartBackgroundConnection()
        {
            if (_autoConnectStarted) return;
            _autoConnectStarted = true;

            Task.Run(async () =>
            {
                try
                {
                    string savedPassword = await SecureStorage.GetPasswordAsync("OBSGameBar", "WebSocket");
                    if (!string.IsNullOrEmpty(savedPassword))
                    {
                        Settings.Password = savedPassword;
                    }
                }
                catch { }

                if (Settings.AutoConnect)
                {
                    await ConnectionManager.StartAutoConnectAsync();
                }
            });
        }
    }
}






