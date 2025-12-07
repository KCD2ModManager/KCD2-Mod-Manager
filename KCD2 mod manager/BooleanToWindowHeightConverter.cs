using System;
using System.Globalization;
using System.Windows.Data;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Converter: Boolean -> Window Height (true = 320 für multiline, false = 220 für single-line)
    /// </summary>
    public class BooleanToWindowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isMultiline && isMultiline)
            {
                return 320.0; // Höhere Fensterhöhe für multiline
            }
            return 220.0; // Standard-Fensterhöhe für single-line
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

