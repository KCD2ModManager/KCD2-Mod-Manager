using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KCD2_mod_manager
{
    public class WorkshopActionVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return Visibility.Visible;
            }

            bool isWorkshop = values[0] is bool workshop && workshop;
            bool allowActions = values[1] is bool allow && allow;

            return (!isWorkshop || allowActions) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
