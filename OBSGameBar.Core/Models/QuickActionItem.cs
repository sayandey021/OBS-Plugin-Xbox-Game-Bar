using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OBSGameBar.Core.Models
{
    public enum QuickActionType
    {
        ToggleStream,
        StartStream,
        StopStream,
        ToggleRecord,
        StartRecord,
        StopRecord,
        ToggleRecordPause,
        SaveReplay,
        ToggleReplayBuffer,
        ToggleMic,
        MuteMic,
        UnmuteMic,
        ToggleDesktop,
        MuteDesktop,
        TriggerTransition,
        ToggleVirtualCam,
        ToggleStudioMode
    }

    public class QuickActionItem : INotifyPropertyChanged
    {
        private int _slotIndex;
        private QuickActionType _actionType;
        private string _label;
        private string _iconGlyph;
        private bool _isActive;
        private string _targetParameter;

        public int SlotIndex
        {
            get => _slotIndex;
            set => SetField(ref _slotIndex, value);
        }

        public QuickActionType ActionType
        {
            get => _actionType;
            set
            {
                if (SetField(ref _actionType, value))
                {
                    UpdateDefaultPresentation();
                }
            }
        }

        public string Label
        {
            get => _label;
            set => SetField(ref _label, value);
        }

        public string IconGlyph
        {
            get => _iconGlyph;
            set => SetField(ref _iconGlyph, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetField(ref _isActive, value);
        }

        public string TargetParameter
        {
            get => _targetParameter;
            set => SetField(ref _targetParameter, value);
        }

        public QuickActionItem() { }

        public QuickActionItem(int slotIndex, QuickActionType actionType)
        {
            SlotIndex = slotIndex;
            ActionType = actionType;
            UpdateDefaultPresentation();
        }

        public void UpdateDefaultPresentation()
        {
            switch (ActionType)
            {
                case QuickActionType.ToggleStream:
                    Label = "Toggle Stream";
                    IconGlyph = "\uE714"; // Broadcast
                    break;
                case QuickActionType.StartStream:
                    Label = "Start Stream";
                    IconGlyph = "\uE768"; // Play
                    break;
                case QuickActionType.StopStream:
                    Label = "Stop Stream";
                    IconGlyph = "\uE71A"; // Stop
                    break;
                case QuickActionType.ToggleRecord:
                    Label = "Toggle Record";
                    IconGlyph = "\uE7C8"; // Video
                    break;
                case QuickActionType.StartRecord:
                    Label = "Start Record";
                    IconGlyph = "\uE7C8";
                    break;
                case QuickActionType.StopRecord:
                    Label = "Stop Record";
                    IconGlyph = "\uE71A";
                    break;
                case QuickActionType.ToggleRecordPause:
                    Label = "Pause/Resume";
                    IconGlyph = "\uE769"; // Pause
                    break;
                case QuickActionType.SaveReplay:
                    Label = "Save Replay";
                    IconGlyph = "\uE777"; // Save
                    break;
                case QuickActionType.ToggleReplayBuffer:
                    Label = "Replay Buffer";
                    IconGlyph = "\uE7A7"; // Rotate
                    break;
                case QuickActionType.ToggleMic:
                    Label = "Toggle Mic";
                    IconGlyph = "\uE720"; // Microphone
                    break;
                case QuickActionType.MuteMic:
                    Label = "Mute Mic";
                    IconGlyph = "\uE74F"; // Mute
                    break;
                case QuickActionType.UnmuteMic:
                    Label = "Unmute Mic";
                    IconGlyph = "\uE720";
                    break;
                case QuickActionType.ToggleDesktop:
                    Label = "Toggle Audio";
                    IconGlyph = "\uE767"; // Volume
                    break;
                case QuickActionType.MuteDesktop:
                    Label = "Mute Audio";
                    IconGlyph = "\uE74F";
                    break;
                case QuickActionType.TriggerTransition:
                    Label = "Transition";
                    IconGlyph = "\uE72C"; // Refresh / arrows
                    break;
                case QuickActionType.ToggleVirtualCam:
                    Label = "Virtual Cam";
                    IconGlyph = "\uE960"; // Camera
                    break;
                case QuickActionType.ToggleStudioMode:
                    Label = "Studio Mode";
                    IconGlyph = "\uE7F4"; // Split
                    break;
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
    }
}

