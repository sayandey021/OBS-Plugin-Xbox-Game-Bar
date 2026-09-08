# OBS Game Bar — Xbox Game Bar Widget for OBS Studio

A polished, lightweight, native **Xbox Game Bar widget for Windows 10 and 11** that lets you control OBS Studio directly from the Xbox Game Bar overlay (`Win + G`) without alt-tabbing out of your game.

---

## Features

- **Native Xbox Game Bar Integration**: Registered via official Microsoft Xbox Game Bar SDK (`microsoft.gameBarUIExtension`) as both a standard widget and dedicated settings flyout.
- **obs-websocket v5 Protocol**: Direct WebSocket connection with SHA-256 challenge-response authentication, real-time event subscriptions, and request/response correlation.
- **Live Stream Controls**:
  - Start, Stop, and Toggle Stream.
  - Live indicators with duration (`hh:mm:ss`), current bitrate (`Mbps`), and dropped frame percentage.
  - Optional confirmation safety dialog before starting/stopping.
- **Recording Controls**:
  - Start, Stop, Pause, Resume, and Toggle Recording.
  - Live recording timecode and status badge (`● REC` / `PAUSED`).
  - Optional confirmation prompt before stopping.
- **Prominent Replay Buffer**:
  - Large, high-priority **`SAVE REPLAY`** button designed for fast gaming reaction time.
  - Start/Stop Replay Buffer with live ready state indicator and buffer duration.
- **Dynamic Scene Switcher**:
  - Automatically loads scenes from OBS Studio.
  - Visual highlight for the currently active program scene.
  - Instant scene switching on click.
- **Source Visibility Toggle**:
  - Shows all sources in the currently active scene.
  - Checkbox toggles to immediately hide or show sources (webcam, overlays, game capture, alerts).
  - Synchronizes with OBS scene changes, source additions, and removals.
- **Debounced Audio Mixer**:
  - Sliders for all OBS audio inputs (Microphone, Desktop Audio, etc.).
  - Real-time dB indicator (e.g. `-12.4 dB`).
  - Mute/unmute toggle button with visual volume icon.
  - **Coalesced 50ms debouncing** prevents flooding WebSocket requests while dragging sliders.
- **Studio Mode Support**:
  - Displays Preview and Program scenes side-by-side.
  - One-click `TRANSITION` button.
  - Automatically activates only when Studio Mode is enabled in OBS.
- **Customizable Quick Actions Grid**:
  - 8-slot gaming grid for 1-click execution: Stream, Record, Save Replay, Buffer, Mic Mute, Audio Mute, Transitions, Virtual Camera.
- **Hardware-Backed Credential Security**:
  - Passwords are encrypted and stored in `Windows.Security.Credentials.PasswordVault`.
  - Passwords are never saved in plaintext and never printed in logs.
- **Gaming-Optimized Performance**:
  - **Zero CPU overhead** when Game Bar is hidden: automatically pauses timers and WebSocket polling via `XboxGameBarWidget.VisibleChanged`.
  - Event-driven updates instead of high-frequency polling.
  - Exponential backoff reconnection (1s, 2s, 4s, 8s, 16s) when OBS is offline or restarted.

---

## Architecture

```
Xbox Game Bar Overlay (Win + G)
  │
  ├── App.xaml / App.xaml.cs (ms-gamebarwidget protocol activation & widget lifecycle)
  │     ├── OBSControlWidget (MainWidgetView.xaml)
  │     └── OBSControlSettings (SettingsView.xaml)
  │
  ├── OBSGameBar (UWP Presentation Layer)
  │     ├── Fluent Dark Theme (FluentResources.xaml)
  │     ├── Converters & UI Dispatcher
  │     ├── UwpPasswordVaultStorage (Windows.Security.Credentials.PasswordVault)
  │     └── GameBarNotificationService (Windows Toast Notifications)
  │
  └── OBSGameBar.Core (.NET Standard 2.0 / .NET 10)
        ├── Models (ObsState, SceneModel, SourceModel, AudioInputModel, QuickActionItem)
        ├── Protocol (ObsWebSocket v5 OpCodes, SHA-256 Auth, Request Envelopes)
        ├── Services:
        │     ├── ObsWebSocketService (System.Net.WebSockets.ClientWebSocket)
        │     ├── ObsConnectionManager (Auto-connect & Exponential Backoff)
        │     ├── ObsStreamService
        │     ├── ObsRecordingService
        │     ├── ObsReplayService
        │     ├── ObsSceneService
        │     ├── ObsSourceService
        │     └── ObsAudioService (Debounced volume slider updates)
        └── ViewModels (MainWidgetViewModel, SettingsViewModel, AudioInputViewModel)
```

---

## OBS Studio Configuration

1. Launch **OBS Studio** (v28 or later, which includes `obs-websocket v5` built-in).
2. Go to **Tools** → **WebSocket Server Settings**.
3. Check **Enable WebSocket server**.
4. Set **Server Port** to `4455` (default).
5. (Optional but recommended) Check **Enable Authentication** and set a password.
6. Click **Apply** and **OK**.

---

## Building and Installing the Widget

### Prerequisites
- Windows 10 build 19041+ or Windows 11.
- Visual Studio 2022 / 2026 with the **Universal Windows Platform development** workload installed.
- **Windows Developer Mode** enabled:
  - Open Windows **Settings** → **Update & Security** (or **System**) → **For developers**.
  - Toggle **Developer Mode** to **On**.

### 1. Build Solution via MSBuild
Run the following in PowerShell:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" "OBSGameBar.sln" /p:Configuration=Debug /p:Platform=x64
```

### 2. Sideload and Register into Xbox Game Bar
The build generates an installation package in `OBSGameBar\AppPackages`:

```powershell
# Open PowerShell as Administrator and run the generated installer
cd "OBSGameBar\AppPackages\OBSGameBar_1.0.0.0_x64_Debug_Test"
.\Install.ps1
```

Once installed, Windows registers the app extension with Xbox Game Bar automatically.

### 3. Open in Xbox Game Bar
1. Press `Win + G` on your keyboard to bring up the Xbox Game Bar overlay.
2. Click the **Widget Menu** icon in the Game Bar top bar.
3. Select **OBS Studio**.
4. (Optional) Click the **Pin** icon on the widget header to keep it visible on top of your games while playing!

---

## Running Unit Tests

The solution includes an automated test suite (`OBSGameBar.Tests`) covering protocol framing, SHA-256 authentication hashing, state synchronization, volume debouncing, and connection backoff:

```powershell
dotnet test OBSGameBar.Tests/OBSGameBar.Tests.csproj
```

All 20 unit tests execute in ~250ms.

---

## License

MIT License. Designed and built with Microsoft Fluent design principles for high-performance gaming.
