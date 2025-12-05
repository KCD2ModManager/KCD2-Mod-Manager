using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für die Erkennung und Verwaltung von Spiel-Installationen
    /// </summary>
    public interface IGameInstallService
    {
        /// <summary>
        /// Erkannte Spiel-Installationen
        /// </summary>
        GameInstallType InstallType { get; }

        /// <summary>
        /// Aktuell ausgewähltes Spiel
        /// </summary>
        GameType SelectedGame { get; set; }

        /// <summary>
        /// Installations-Deskriptor für KCD1 (falls installiert)
        /// </summary>
        GameInstallDescriptor? KCD1Install { get; }

        /// <summary>
        /// Installations-Deskriptor für KCD2 (falls installiert)
        /// </summary>
        GameInstallDescriptor? KCD2Install { get; }

        /// <summary>
        /// Aktuell ausgewählte Installation
        /// </summary>
        GameInstallDescriptor? SelectedInstall { get; }

        /// <summary>
        /// Erkennt installierte Spiele automatisch
        /// </summary>
        Task DetectInstalledGamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Ermittelt die Version eines installierten Spiels
        /// </summary>
        Task<string?> GetGameVersionAsync(GameType gameType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prüft Kompatibilität zwischen Mod-Version und Spiel-Version
        /// </summary>
        Task<bool> IsModCompatibleAsync(string modGameVersion, GameType gameType, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Typ der erkannten Installationen
    /// </summary>
    public enum GameInstallType
    {
        Unknown,
        KCD1,
        KCD2,
        Both
    }

    /// <summary>
    /// Typ des ausgewählten Spiels
    /// </summary>
    public enum GameType
    {
        KCD1,
        KCD2
    }
}

