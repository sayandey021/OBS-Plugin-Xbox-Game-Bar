using System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace OBSGameBar.Views.Converters
{
    public class ActivePillBackgroundConverter : IValueConverter
    {
        private static readonly Color ActiveColor = Color.FromArgb(255, 210, 213, 218); // #D2D5DA (Exact stock Game Bar light pill)
        private static readonly Color InactiveColor = Color.FromArgb(255, 43, 46, 51);   // #2B2E33
        private static readonly Color AccentActiveColor = Color.FromArgb(255, 76, 194, 255); // #4CC2FF

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isActive = value is bool b && b;
            if (isActive)
            {
                if (parameter is string param && param.Equals("Accent", StringComparison.OrdinalIgnoreCase))
                {
                    return new SolidColorBrush(AccentActiveColor);
                }
                return new SolidColorBrush(ActiveColor);
            }
            return new SolidColorBrush(InactiveColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class ActivePillForegroundConverter : IValueConverter
    {
        private static readonly Color ActiveTextColor = Color.FromArgb(255, 37, 40, 44);   // #25282C (Stock titlebar dark text)
        private static readonly Color InactiveTextColor = Color.FromArgb(255, 255, 255, 255); // White text

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isActive = value is bool b && b;
            return new SolidColorBrush(isActive ? ActiveTextColor : InactiveTextColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class ActivePillBorderConverter : IValueConverter
    {
        private static readonly Color InactiveBorderColor = Color.FromArgb(255, 61, 66, 74); // #3D424A

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isActive = value is bool b && b;
            return new SolidColorBrush(isActive ? Colors.Transparent : InactiveBorderColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class ScenePillBackgroundConverter : IValueConverter
    {
        private static readonly Color ActiveProgramColor = Color.FromArgb(255, 210, 213, 218); // #D2D5DA (Normal Mode active pill)
        private static readonly Color PreviewColor = Color.FromArgb(255, 31, 54, 77);         // #1F364D (Studio Mode Preview staged pill)
        private static readonly Color InactiveColor = Color.FromArgb(255, 43, 46, 51);        // #2B2E33

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string state = (value is OBSGameBar.Core.Models.SceneModel scene) ? scene.SceneState : (value as string);
            if (state == "NormalActive" || state == "Live") return new SolidColorBrush(ActiveProgramColor);
            if (state == "StudioPreview" || state == "Preview" || state == "StudioLivePreview" || state == "LivePreview")
                return new SolidColorBrush(PreviewColor);
            if (value is bool b && b) return new SolidColorBrush(ActiveProgramColor);
            return new SolidColorBrush(InactiveColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class ScenePillForegroundConverter : IValueConverter
    {
        private static readonly Color ActiveProgramTextColor = Color.FromArgb(255, 37, 40, 44); // #25282C dark
        private static readonly Color PreviewTextColor = Color.FromArgb(255, 76, 194, 255);     // #4CC2FF Fluent cyan
        private static readonly Color InactiveTextColor = Color.FromArgb(255, 255, 255, 255);   // White

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string state = (value is OBSGameBar.Core.Models.SceneModel scene) ? scene.SceneState : (value as string);
            if (state == "NormalActive" || state == "Live") return new SolidColorBrush(ActiveProgramTextColor);
            if (state == "StudioPreview" || state == "Preview" || state == "StudioLivePreview" || state == "LivePreview")
                return new SolidColorBrush(PreviewTextColor);
            if (value is bool b && b) return new SolidColorBrush(ActiveProgramTextColor);
            return new SolidColorBrush(InactiveTextColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class ScenePillBorderConverter : IValueConverter
    {
        private static readonly Color PreviewBorderColor = Color.FromArgb(255, 76, 194, 255); // #4CC2FF cyan
        private static readonly Color LiveBorderColor = Color.FromArgb(255, 196, 43, 28);    // #C42B1C red
        private static readonly Color InactiveBorderColor = Color.FromArgb(255, 61, 66, 74);   // #3D424A

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string state = (value is OBSGameBar.Core.Models.SceneModel scene) ? scene.SceneState : (value as string);
            if (state == "StudioPreview" || state == "Preview" || state == "StudioLivePreview" || state == "LivePreview")
                return new SolidColorBrush(PreviewBorderColor);
            if (state == "StudioLive") return new SolidColorBrush(LiveBorderColor);
            if (state == "NormalActive" || state == "Live") return new SolidColorBrush(Colors.Transparent);
            if (value is bool b && b) return new SolidColorBrush(Colors.Transparent);
            return new SolidColorBrush(InactiveBorderColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class SceneBadgeBackgroundConverter : IValueConverter
    {
        private static readonly Color LiveBadgeColor = Color.FromArgb(255, 196, 43, 28); // Red #C42B1C
        private static readonly Color PreviewBadgeColor = Color.FromArgb(255, 0, 120, 212); // Blue #0078D4

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string state = (value is OBSGameBar.Core.Models.SceneModel scene) ? scene.SceneState : (value as string);
            if (state == "StudioLive" || state == "Live" || state == "StudioLivePreview" || state == "LivePreview")
                return new SolidColorBrush(LiveBadgeColor);
            if (state == "StudioPreview" || state == "Preview")
                return new SolidColorBrush(PreviewBadgeColor);
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class StudioModeBadgeBgConverter : IValueConverter
    {
        private static readonly Color ActiveBgColor = Color.FromArgb(255, 30, 50, 70); // Subtle Fluent blue
        private static readonly Color InactiveBgColor = Color.FromArgb(255, 43, 46, 51); // #2B2E33

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool active = value is bool b && b;
            return new SolidColorBrush(active ? ActiveBgColor : InactiveBgColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    public class StudioModeBadgeFgConverter : IValueConverter
    {
        private static readonly Color ActiveFgColor = Color.FromArgb(255, 76, 194, 255); // #4CC2FF
        private static readonly Color InactiveFgColor = Color.FromArgb(255, 160, 164, 172);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool active = value is bool b && b;
            return new SolidColorBrush(active ? ActiveFgColor : InactiveFgColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
/// <summary>
    /// Maps a QuickActionItem slot index (0-based) to its grid ROW (slot / 2) so an
    /// 8-slot list lays out as a 2-column grid. Slots 0-1 -> row 0, 2-3 -> row 1, etc.
    /// </summary>
    public class QuickActionRowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int slot = value is int i ? i : 0;
            return slot / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    /// <summary>
    /// Maps a QuickActionItem slot index (0-based) to its grid COLUMN (slot % 2) so an
    /// 8-slot list lays out as a 2-column grid. Even slots -> column 0, odd slots -> column 1.
    /// </summary>
    public class QuickActionColumnConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int slot = value is int i ? i : 0;
            return slot % 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    /// <summary>
    /// Returns danger red (#FF4343) if audio source is muted, or secondary text color (#C5C8CD) if unmuted.
    /// </summary>
    public class MutedToBrushConverter : IValueConverter
    {
        private static readonly Color MutedDangerColor = Color.FromArgb(255, 255, 67, 67); // #FF4343 Red
        private static readonly Color NormalColor = Color.FromArgb(255, 197, 200, 205);      // #C5C8CD SecondaryText

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isMuted = value is bool b && b;
            return new SolidColorBrush(isMuted ? MutedDangerColor : NormalColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }

    /// <summary>
    /// Dims the volume slider opacity to 0.35 when muted, 1.0 when active.
    /// </summary>
    public class MutedToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isMuted = value is bool b && b;
            return isMuted ? 0.35 : 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}

