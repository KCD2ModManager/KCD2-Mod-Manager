using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Converter: Boolean -> Height (true = 120 für multiline, false = 50 für single-line)
    /// </summary>
    public class BooleanToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMultiline && isMultiline)
            {
                return 120.0; // Höhere Höhe für multiline
            }
            return 50.0; // Standard-Höhe für single-line
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

