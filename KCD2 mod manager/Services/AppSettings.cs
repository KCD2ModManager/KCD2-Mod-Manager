namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Implementierung von IAppSettings basierend auf Settings.Default
    /// </summary>
    public class AppSettings : IAppSettings
    {
        public string GamePath
        {
            get => Settings.Default.GamePath;
            set => Settings.Default.GamePath = value;
        }

        public string GameLaunchArgs
        {
            get => Settings.Default.GameLaunchArgs;
            set => Settings.Default.GameLaunchArgs = value;
        }

        public bool IsDarkMode
        {
            get => Settings.Default.IsDarkMode;
            set => Settings.Default.IsDarkMode = value;
        }

        public bool IsDevMode
        {
            get => Settings.Default.IsDevMode;
            set => Settings.Default.IsDevMode = value;
        }

        public bool AskOnDelete
        {
            get => Settings.Default.AskOnDelete;
            set => Settings.Default.AskOnDelete = value;
        }

        public bool EnableUpdateNotifications
        {
            get => Settings.Default.EnableUpdateNotifications;
            set => Settings.Default.EnableUpdateNotifications = value;
        }

        public bool ModOrderEnabled
        {
            get => Settings.Default.ModOrderEnabled;
            set => Settings.Default.ModOrderEnabled = value;
        }

        public bool CreateBackup
        {
            get => Settings.Default.CreateBackup;
            set => Settings.Default.CreateBackup = value;
        }

        public bool BackupOnStartup
        {
            get => Settings.Default.BackupOnStartup;
            set => Settings.Default.BackupOnStartup = value;
        }

        public int BackupMaxCount
        {
            get => Settings.Default.BackupMaxCount;
            set => Settings.Default.BackupMaxCount = value;
        }

        public string NexusUserToken
        {
            get => Settings.Default.NexusUserToken;
            set => Settings.Default.NexusUserToken = value;
        }

        public string NexusUsername
        {
            get => Settings.Default.NexusUsername;
            set => Settings.Default.NexusUsername = value;
        }

        public string NexusUserEmail
        {
            get => Settings.Default.NexusUserEmail;
            set => Settings.Default.NexusUserEmail = value;
        }

        public long NexusUserID
        {
            get => Settings.Default.NexusUserID;
            set => Settings.Default.NexusUserID = value;
        }

        public bool NexusIsPremium
        {
            get => Settings.Default.NexusIsPremium;
            set => Settings.Default.NexusIsPremium = value;
        }

        public int WindowWidth
        {
            get => Settings.Default.WindowWidth;
            set => Settings.Default.WindowWidth = value;
        }

        public int WindowHeight
        {
            get => Settings.Default.WindowHeight;
            set => Settings.Default.WindowHeight = value;
        }

        public int WindowLeft
        {
            get => Settings.Default.WindowLeft;
            set => Settings.Default.WindowLeft = value;
        }

        public int WindowTop
        {
            get => Settings.Default.WindowTop;
            set => Settings.Default.WindowTop = value;
        }

        public string WindowState
        {
            get => Settings.Default.WindowState;
            set => Settings.Default.WindowState = value;
        }

        public string? Language
        {
            get => Settings.Default.Language;
            set => Settings.Default.Language = value;
        }

        private string? _lastSelectedGame;
        public string? LastSelectedGame
        {
            get => _lastSelectedGame;
            set => _lastSelectedGame = value;
        }

        private string? _lastUsedProfile_KCD1;
        public string? LastUsedProfile_KCD1
        {
            get => _lastUsedProfile_KCD1;
            set => _lastUsedProfile_KCD1 = value;
        }

        private string? _lastUsedProfile_KCD2;
        public string? LastUsedProfile_KCD2
        {
            get => _lastUsedProfile_KCD2;
            set => _lastUsedProfile_KCD2 = value;
        }

        public string GamePath_KCD1
        {
            get => Settings.Default.GamePath_KCD1;
            set => Settings.Default.GamePath_KCD1 = value;
        }

        public string GamePath_KCD2
        {
            get => Settings.Default.GamePath_KCD2;
            set => Settings.Default.GamePath_KCD2 = value;
        }

        public void Save()
        {
            Settings.Default.Save();
        }
    }
}

