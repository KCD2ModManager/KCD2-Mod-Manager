using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager
{
    public class HighlightColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not HighlightColorData data)
            {
                return Brushes.Transparent;
            }

            string hex = Settings.Default.IsDarkMode ? data.Dark : data.Light;
            if (string.IsNullOrWhiteSpace(hex))
            {
                return Brushes.Transparent;
            }

            try
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
                brush.Freeze();
                return brush;
            }
            catch
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
