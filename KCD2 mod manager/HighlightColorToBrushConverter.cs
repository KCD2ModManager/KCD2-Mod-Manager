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
                var sourceBrush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
                var color = sourceBrush.Color;
                // Keep original light-mode look, but make dark-mode highlights a bit stronger
                // while still avoiding hard contrast jumps.
                byte maxAlpha = Settings.Default.IsDarkMode ? (byte)84 : (byte)255;
                byte alpha = color.A == 255 ? maxAlpha : (byte)Math.Min(color.A, maxAlpha);
                var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
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
