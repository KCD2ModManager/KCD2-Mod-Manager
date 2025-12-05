using System;
using System.Globalization;
using System.Windows.Data;
using KCD2_mod_manager.Services;

namespace KCD2_mod_manager
{
    /// <summary>
    /// Converter für GameType-Vergleich mit TwoWay-Binding-Unterstützung
    /// 
    /// WICHTIG: Dieser Converter wird für RadioButtons verwendet, die TwoWay-Bindings benötigen.
    /// ConvertBack wird aufgerufen, wenn der Benutzer einen RadioButton aktiviert (IsChecked = true).
    /// Nur wenn der Wert true ist, wird der Parameter (GameType) zurückgegeben.
    /// </summary>
    public class GameTypeEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GameType gameType)
            {
                // Parameter kann String ("KCD1", "KCD2") oder direkt GameType sein
                if (parameter is string paramStr)
                {
                    if (Enum.TryParse<GameType>(paramStr, out var paramGameType))
                    {
                        return gameType == paramGameType;
                    }
                }
                else if (parameter is GameType paramGameType)
                {
                    return gameType == paramGameType;
                }
            }
            return false;
        }

        /// <summary>
        /// ConvertBack wird aufgerufen, wenn der Benutzer einen RadioButton aktiviert.
        /// Nur wenn value == true (RadioButton wird aktiviert), geben wir den GameType zurück.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Nur verarbeiten, wenn der Benutzer den RadioButton aktiviert (IsChecked = true)
            if (value is bool isChecked && isChecked)
            {
                // Parameter kann direkt GameType sein
                if (parameter is GameType gt)
                {
                    return gt;
                }

                // Oder Parameter ist ein String ("KCD1", "KCD2")
                if (parameter is string s && Enum.TryParse<GameType>(s, out var parsed))
                {
                    return parsed;
                }

                // Wenn Parameter nicht erkannt werden kann, nichts ändern
                return Binding.DoNothing;
            }

            // Wenn RadioButton deaktiviert wird (false), nichts zurückgeben
            return Binding.DoNothing;
        }
    }
}

