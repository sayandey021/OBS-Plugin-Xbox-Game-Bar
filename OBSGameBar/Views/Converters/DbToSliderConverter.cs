using System;
using Windows.UI.Xaml.Data;

namespace OBSGameBar.Views.Converters
{
    public class DbToSliderConverter : IValueConverter
    {
        private const float MinDb = -60.0f;
        private const float MaxDb = 0.0f;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is float db)
            {
                if (db <= MinDb || float.IsNegativeInfinity(db)) return 0.0;
                if (db >= MaxDb) return 100.0;

                double percent = (db - MinDb) / (MaxDb - MinDb);
                return Math.Max(0.0, Math.Min(100.0, percent * 100.0));
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is double sliderVal)
            {
                if (sliderVal <= 0.5) return -100.0f; // Mute threshold
                float db = (float)((sliderVal / 100.0) * (MaxDb - MinDb) + MinDb);
                return (float)Math.Round(db, 1);
            }
            return 0.0f;
        }
    }
}
