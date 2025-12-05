namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für Anwendungseinstellungen
    /// </summary>
    public interface IAppSettings
    {
        string GamePath { get; set; }
        string GameLaunchArgs { get; set; }
        bool IsDarkMode { get; set; }
        bool IsDevMode { get; set; }
        bool AskOnDelete { get; set; }
        bool EnableUpdateNotifications { get; set; }
        bool ModOrderEnabled { get; set; }
        bool CreateBackup { get; set; }
        bool BackupOnStartup { get; set; }
        int BackupMaxCount { get; set; }
        string NexusUserToken { get; set; }
        string NexusUsername { get; set; }
        string NexusUserEmail { get; set; }
        long NexusUserID { get; set; }
        bool NexusIsPremium { get; set; }
        int WindowWidth { get; set; }
        int WindowHeight { get; set; }
        int WindowLeft { get; set; }
        int WindowTop { get; set; }
        string WindowState { get; set; }
        string? Language { get; set; }
        string? LastSelectedGame { get; set; } // Feature 11: KCD1 oder KCD2
        string? LastUsedProfile_KCD1 { get; set; } // Letztes verwendetes Profil für KCD1
        string? LastUsedProfile_KCD2 { get; set; } // Letztes verwendetes Profil für KCD2

        void Save();
    }
}

