using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Converter der true zurückgibt, wenn der Wert nicht null ist
    /// </summary>
    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

