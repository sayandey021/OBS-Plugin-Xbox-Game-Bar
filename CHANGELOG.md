# Changelog

All notable changes to the **OBS Game Bar Widget** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [0.3.5] - 2026-09-08

### Added
- **Windows 11 Volume Slider Knob Expansion & Halo Effect**:
  - Implemented exact Windows 11 Fluent audio slider aesthetics matching native Quick Settings and Volume Mixer.
  - **Knob Hidden at Rest**: Knob remains completely hidden (`Opacity="0"`) when the pointer is outside the slider area, showing a clean continuous track.
  - **Slider Hover**: Hovering over the slider reveals the cyan knob (`#4CC2FF`, diameter 9px).
  - **Knob Hover & Press Expansion**: Hovering directly over the knob smoothly reveals a 22px circular dark halo (`#454545`) and expands the blue circle by 35% (from 9px to ~12.2px) using hardware-accelerated `ScaleTransform` animations.
- **Windows 11 Rounded Pill ScrollBar**:
  - Re-implemented the ScrollBar control template with all required UWP named parts (`VerticalRoot`, `VerticalThumb`, etc.) featuring a true rounded pill border (`CornerRadius="3"`).
  - Configured subtle pill appearance at rest that widens smoothly on hover from 3px to 6px.
- **Stream and Record Button Shared-Column Alignment**:
  - Unified the Stream and Recording card rows into a single flat Grid with shared column definitions (`Column 1: 126px`), geometrically locking the left edges of "Start Stream" and "Start Record".

---

## [0.3.4] - 2026-09-08

### Added
- **Windows 11 Fluent Slider Design with Hover Reveal**:
  - Implemented the authentic Windows 11 Volume Slider control template matching stock Game Bar and Windows 11 audio controls (slim 4px rounded track, vibrant cyan `#4CC2FF` progress fill, and 14px circular thumb with dark outline).
  - **Dynamic Hover Reveal**: The circular thumb remains hidden at rest (revealing a clean, continuous bar) and smoothly fades in on hover (`PointerOver`) and press (`Pressed`), perfectly matching native Windows 11 behavior.
- **Windows 11 Rounded ScrollBar Styling**:
  - Configured transparent track backgrounds, 4px corner radius, and subtle Fluent dark pill thumbs (`#44C5C8CD` resting, `#77C5C8CD` hover) across all widget scroll areas.
- **Pixel-Perfect Action Button Alignment**:
  - Re-architected the action buttons in the Stream and Recording cards with uniform dimensions (`Width="126"`, `Height="32"`, `Padding="0"`) and a 2-column centered Grid (`Column 0: 18px icon`, `Column 1: text with 6px margin`).
  - Both "Start Stream" and "Start Record" left and right borders now align with mathematical precision, with icons and labels starting at identical X coordinates.

---

## [0.3.3] - 2026-09-08

### Fixed
- **Runtime COM Interface Error (`0x80004002` / `E_NOINTERFACE` / `InvalidCastException`)**:
  - Resolved crash occurring during OBS connection synchronization when the dashboard `ScrollViewer` transitioned to `Visible`.
  - Replaced incomplete custom `ControlTemplate` overrides for `Slider` and `ScrollBar` in `FluentResources.xaml` with clean, native WinUI property setters, eliminating missing internal WinRT template part interface query failures.
  - Fixed raw boolean binding on Replay Buffer status label in `MainWidgetView.xaml` (`IsReplayBufferActive`) by converting it into discrete `Active` and `Ready` status text blocks with `BoolToVisConverter` and `InverseBoolToVisConverter`.
- **Quick Actions Direct Command Architecture**:
  - Switched Quick Action buttons to direct, dedicated view model commands (`ToggleStreamCommand`, `ToggleRecordCommand`, `SaveReplayCommand`, `ToggleReplayBufferCommand`, `ToggleMicCommand`, `ToggleDesktopCommand`, `TriggerTransitionCommand`, `ToggleVirtualCamCommand`).
- **Compiled Binding Value Converter Registration**:
  - Directly registered all 18 value converters inside `<Application.Resources>` in `App.xaml` and local page dictionaries in `MainWidgetView.xaml` and `SettingsView.xaml` to guarantee instantaneous resolution for `{x:Bind LookupConverter}`.
- **Model Resolution in UI Event Handlers**:
  - Refactored `OnSceneButtonClicked` and `OnSourceToggled` in `MainWidgetView.xaml.cs` to safely resolve `DataContext as SceneModel` / `DataContext as SourceModel`.

---

## [0.3.2] - 2026-09-08

### Added
- **Scene-Specific Audio Mixer Filtering**:
  - The Audio Mixer now dynamically displays only the audio sliders belonging to the **currently active scene** (plus Global Audio Devices like *Desktop Audio* and *Mic/Aux 1–4* queried via `GetSpecialInputs`), perfectly mirroring OBS Studio's native Audio Mixer dock.
  - Non-global audio sources belonging to other scenes (e.g., `Media 2` in `Scene 2` while viewing `Scene`) are automatically filtered out.
  - Group items and nested scene items are traversed and included in the scene audio filter.
- **Dynamic Audio Mixer Scene Synchronization**:
  - Switching scenes in the widget or in OBS Studio instantly refreshes the Audio Mixer list to display the newly selected scene's audio sources.
  - Auto-refreshes on `SceneItemCreated`, `SceneItemRemoved`, and `SceneItemListReindexed` events.
- **Unit Test Coverage**: Added unit tests for scene-filtered audio inputs and dynamic scene switching updates (35 passing tests total).

---

## [0.3.1] - 2026-09-08

### Added
- **Audio Mixer Mute Visual Feedback**:
  - Added `MutedToBrushConverter` rendering mute icon in bold danger red (`#FF4343`) when muted, and `#C5C8CD` when active.
  - Added `MutedToOpacityConverter` dimming volume slider track and dB readout to `0.35` opacity when muted.
- **Quick Action Virtual Camera Handler**: Implemented `ToggleVirtualCam` quick action via `ObsWebSocketService.SendRequestAsync("ToggleVirtualCam")`.
- **Audio & Quick Action Unit Tests**: Added unit tests for case-insensitive `ToggleInputMuteAsync`, `InputMuteStateChanged`, and `ToggleVirtualCam` quick action execution (33 passing tests total).

### Changed
- **Quick Actions Grid Layout**: Replaced `ItemsControl` Grid panel with an explicit 2-column, 4-row responsive `Grid` (`ColumnSpacing="6"`, `RowSpacing="6"`, 50%/50% width distribution), ensuring all 8 quick action slots render side-by-side with zero overlap.

### Fixed
- **Quick Actions Button Overlap**: Fixed critical XAML bug where `ItemsControl` wrapped each `Button` in a `ContentPresenter`, ignoring `Grid.Row`/`Grid.Column` and stacking all 8 buttons into Row 0, Column 0.
- **Audio Mute State Synchronization & UI Refresh**: Fixed issue where muting/unmuting an audio input (such as `Media`) had no discernible visual feedback on the widget UI, and made audio input name resolution case-insensitive.

---

## [0.3.0] - 2026-09-08

### Added
- **Studio Mode Live Status Banner**: Added a compact header banner in the Scenes section showing the staging flow: `[PREVIEW] {PreviewScene} ➔ [LIVE] {ProgramScene}`.
- **Dedicated Scene Badges**: Added explicit `[LIVE]` red badge (`#C42B1C`) and `[PREVIEW]` blue badge (`#0078D4`) with matching border colors (`#C42B1C` for live output, `#4CC2FF` for preview staging).
- **Dual-State Pill Support**: Scenes staged in Preview that are also active on Program display both `[LIVE]` and `[PREVIEW]` badges simultaneously.
- **Dynamic Scene Sources Header**: Header dynamically updates to `"Scene sources (Preview)"` in Studio Mode and `"Scene sources"` in Normal Mode.
- **`SetSourceVisibilityCommand`**: Dedicated view model command accepting explicit `(SourceModel source, bool isEnabled)` tuples for deterministic state dispatch.
- **`RefreshStudioModeScenesAsync`**: Background synchronization querying both `GetCurrentProgramScene` and `GetCurrentPreviewScene` from OBS WebSocket v5.
- **Expanded Unit Tests**: Added unit tests for explicit source visibility commands, Studio Mode preview staging without auto-transition, and transition synchronization (30 passing tests).

### Changed
- **Studio Mode Scene Pill Interaction**: Clicking a scene pill in Studio Mode now **unconditionally stages it into Preview** (`SetCurrentPreviewSceneAsync`), matching OBS Studio dock behavior.
- **Live Transition Triggering**: Transitions to live output are now exclusively triggered via the `[ ⇄ Transition ]` button in the Scenes header or within OBS Studio.
- **Scene Sources Checkbox Binding**: Changed `IsChecked` binding mode from `Mode=TwoWay` to `Mode=OneWay` in `MainWidgetView.xaml`.
- **Target Scene Resolution**: `ObsSourceService` now falls back to the Preview scene in Studio Mode and Program scene in Normal Mode when `sceneName` is omitted.

### Fixed
- **Scene Source Enable/Disable Inversion Bug**: Fixed issue where clicking a scene source checkbox failed to enable or disable the source in OBS due to a race between TwoWay binding mutation and `!src.IsEnabled` inversion.
- **Studio Mode Event Desynchronization**: Fixed issue where `CurrentProgramSceneChanged` and `SceneTransitionEnded` left stale `IsPreview = true` flags on live scenes.
- **Scene Item Event Filtering**: Added null-safe checks for `src.SceneName` matching in `SceneItemEnableStateChanged`.

---

## [0.2.0] - 2026-09-08

### Added
- **Windows 11 Fluent Dark Theme**: Authentic Windows 11 Game Bar aesthetics matching stock widgets (`#25282C` background, `#2B2E33` card containers, `#3D424A` borders).
- **Stock Game Bar Segmented Pills**: Selected scene pill styled in stock Fluent light (`#D2D5DA` background with `#25282C` dark text).
- **Primary Dispatcher Ownership**: Added primary dispatcher registration in `App.xaml.cs` and `MainWidgetView.xaml.cs` to prioritize the active Game Bar widget view.
- **XAML Binding Fallbacks**: Added `FallbackValue=Collapsed` to active-state elements and `FallbackValue=Visible` to idle elements to eliminate visual artifacts before bindings evaluate.

### Changed
- **Action Button Styling**: Restyled "Save Replay" and header "Transition" buttons from solid cyan blocks to refined Fluent dark buttons (`#2E3238` with 1px border and `#4CC2FF` icon accents).
- **Layout Streamlining**: Removed redundant duplicate "Studio mode" card at the bottom of the widget and integrated controls into the Scenes section.
- **Scene Ordering**: Realigned scene list ordering to reverse bottom-up OBS WebSocket indices, matching OBS Studio's visual top-to-bottom dock order.

### Fixed
- **`RPC_E_WRONG_THREAD` Exception (0x8001010E)**: Resolved crash caused by static readonly `SolidColorBrush` instances across UI threads in `PillStyleConverters.cs` and `ConnectionStatusToBrushConverter.cs`.
- **UI State Ghosting**: Fixed bug where mutually exclusive elements (Start/Stop, Offline/LIVE) were visible simultaneously due to halted XAML binding passes.
- **Duplicate Desktop Window Launching**: Prevented `OnLaunched` from creating a secondary disconnected window when activated via Xbox Game Bar protocol.

---

## [0.1.0] - 2026-09-07

### Added
- **Initial Native Xbox Game Bar Integration**: Registered widget and settings flyout using the official Microsoft Xbox Game Bar SDK (`microsoft.gameBarUIExtension`).
- **obs-websocket v5 Protocol Implementation**:
  - Full client implementation using `System.Net.WebSockets.ClientWebSocket`.
  - SHA-256 challenge-response authentication (`ObsAuthHelper`).
  - Request/response correlation and real-time event subscriptions.
- **Live Stream Controls**:
  - Start, stop, and toggle streaming.
  - Live duration counter, bitrate calculation, and dropped frame percentage.
- **Recording Controls**:
  - Start, stop, pause, resume, and toggle recording.
  - Recording timecode and status indicators (`REC` / `PAUSED`).
- **Replay Buffer Integration**:
  - Quick "Save Replay" button with configurable duration.
  - Start, stop, and toggle replay buffer.
- **Dynamic Scene Switcher**:
  - Automatic scene retrieval from OBS Studio.
  - Single-click scene switching in Normal Mode.
- **Current Scene Sources List**:
  - Real-time enumeration of scene items in the active scene.
  - Source visibility toggle via checkboxes.
- **Debounced Audio Mixer**:
  - Volume sliders with 50ms coalesced debouncing to avoid WebSocket request flooding.
  - Real-time dB readouts and mute toggle buttons.
- **Studio Mode Controls**:
  - Studio Mode detection and transition command support.
- **Quick Actions Grid**:
  - 8-slot action grid for common broadcast hotkey functions.
- **Credential Security**:
  - Encrypted password storage using `Windows.Security.Credentials.PasswordVault`.
- **Power Efficiency**:
  - Automatic pausing of timers and polling when widget visibility is toggled off (`XboxGameBarWidget.VisibleChanged`).
  - Auto-reconnect engine with exponential backoff (1s to 16s).
- **Initial Test Suite**: 26 unit tests verifying WebSocket protocol, authentication, connection state, stream, recording, and audio logic.
