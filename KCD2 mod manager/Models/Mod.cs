using System.Windows;

namespace KCD2_mod_manager.Models
{
    /// <summary>
    /// Repräsentiert einen Mod im Mod-Manager
    /// </summary>
    public class Mod : ViewModelBase
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _version = string.Empty;
        private string _path = string.Empty;
        private bool _isEnabled;
        private string _note = string.Empty;
        private int _number;
        private bool _hasUpdate;
        private string _latestVersion = string.Empty;
        private int _modNumber;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public string Note
        {
            get => _note;
            set => SetProperty(ref _note, value);
        }

        public int Number
        {
            get => _number;
            set => SetProperty(ref _number, value);
        }

        public bool HasUpdate
        {
            get => _hasUpdate;
            set => SetProperty(ref _hasUpdate, value);
        }

        public string LatestVersion
        {
            get => _latestVersion;
            set => SetProperty(ref _latestVersion, value);
        }

        public int ModNumber
        {
            get => _modNumber;
            set => SetProperty(ref _modNumber, value);
        }

        private bool _updateChecksEnabled = true;
        /// <summary>
        /// Gibt an, ob Update-Checks für diesen Mod aktiviert sind
        /// WICHTIG: Wird aus mod_versions.json geladen
        /// </summary>
        public bool UpdateChecksEnabled
        {
            get => _updateChecksEnabled;
            set => SetProperty(ref _updateChecksEnabled, value);
        }

        private bool _isCompatible = true;
        /// <summary>
        /// Feature 9: Gibt an, ob der Mod mit der installierten Spiel-Version kompatibel ist
        /// </summary>
        public bool IsCompatible
        {
            get => _isCompatible;
            set => SetProperty(ref _isCompatible, value);
        }

        private string _gameVersion = string.Empty;
        /// <summary>
        /// Feature 9: Spiel-Version, für die der Mod erstellt wurde
        /// </summary>
        public string GameVersion
        {
            get => _gameVersion;
            set => SetProperty(ref _gameVersion, value);
        }

        public Visibility UpdateVisibility => HasUpdate ? Visibility.Visible : Visibility.Collapsed;
        
        /// <summary>
        /// Feature 9: Sichtbarkeit des Kompatibilitätswarnung-Indikators
        /// </summary>
        public Visibility CompatibilityWarningVisibility => IsCompatible ? Visibility.Collapsed : Visibility.Visible;
    }
}

