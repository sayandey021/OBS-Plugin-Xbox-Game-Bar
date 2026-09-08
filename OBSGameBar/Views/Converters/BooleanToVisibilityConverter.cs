using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace OBSGameBar.Views.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Inverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool bVal = value is bool b && b;
            if (Inverse) bVal = !bVal;
            return bVal ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility vis)
            {
                bool b = vis == Visibility.Visible;
                return Inverse ? !b : b;
            }
            return false;
        }
    }
}
