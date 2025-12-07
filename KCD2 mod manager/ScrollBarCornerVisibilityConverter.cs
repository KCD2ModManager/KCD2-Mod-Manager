using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Converter für ScrollBar-Corner-Sichtbarkeit
    /// Zeigt die Corner nur an, wenn beide ScrollBars sichtbar sind
    /// </summary>
    public class ScrollBarCornerVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Prüfe, ob beide Werte Visibility-Objekte sind
            if (values == null || values.Length != 2)
                return Visibility.Collapsed;

            // Prüfe, ob beide ScrollBars sichtbar sind
            if (values[0] is Visibility verticalVisibility && 
                values[1] is Visibility horizontalVisibility)
            {
                if (verticalVisibility == Visibility.Visible && 
                    horizontalVisibility == Visibility.Visible)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

