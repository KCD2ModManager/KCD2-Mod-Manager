using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Converter: Boolean -> ScrollBarVisibility (true = Auto, false = Disabled)
    /// </summary>
    public class BooleanToScrollBarVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMultiline && isMultiline)
            {
                return ScrollBarVisibility.Auto;
            }
            return ScrollBarVisibility.Disabled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

