using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using OBSGameBar.Core.Models;

namespace OBSGameBar.Views.Converters
{
    public class ConnectionStatusToBrushConverter : IValueConverter
    {
        private static readonly Color ConnectedColor = Color.FromArgb(255, 16, 185, 129);   // Green (#10B981)
        private static readonly Color WarningColor = Color.FromArgb(255, 245, 158, 11);        // Amber/Yellow (#F59E0B)
        private static readonly Color DisconnectedColor = Color.FromArgb(255, 239, 68, 68); // Red (#EF4444)
        private static readonly Color InactiveColor = Color.FromArgb(255, 45, 45, 45);       // Graphite (#2D2D2D)

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return new SolidColorBrush(b ? ConnectedColor : InactiveColor);
            }

            if (value is ObsConnectionStatus status)
            {
                switch (status)
                {
                    case ObsConnectionStatus.Connected:
                        return new SolidColorBrush(ConnectedColor);
                    case ObsConnectionStatus.Connecting:
                    case ObsConnectionStatus.Reconnecting:
                        return new SolidColorBrush(WarningColor);
                    case ObsConnectionStatus.Disconnected:
                    case ObsConnectionStatus.AuthFailed:
                    case ObsConnectionStatus.Error:
                    default:
                        return new SolidColorBrush(DisconnectedColor);
                }
            }
            return new SolidColorBrush(DisconnectedColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
