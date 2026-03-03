using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für die Verwaltung persistenter Mod-Metadaten
    /// </summary>
    public interface IUserModDataService
    {
        /// <summary>
        /// Lädt alle gespeicherten Mod-Metadaten
        /// </summary>
        Task<Dictionary<string, UserModData>> LoadUserModDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Speichert Mod-Metadaten
        /// </summary>
        Task SaveUserModDataAsync(Dictionary<string, UserModData> userModData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Aktualisiert oder erstellt Metadaten für einen Mod
        /// </summary>
        Task SaveModDataAsync(
            string modId,
            string? customVersion = null,
            string? detectedVersion = null,
            string? customNote = null,
            string? customName = null,
            string? categoryId = null,
            bool? ignoredInConflictDetector = null,
            HighlightColorData? highlightColor = null,
            bool updateHighlightColor = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ruft Metadaten für einen spezifischen Mod ab
        /// </summary>
        Task<UserModData?> GetModDataAsync(string modId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Merged Benutzerdaten mit erkannten Manifest-Daten
        /// </summary>
        Task<string> MergeVersionAsync(string modId, string detectedVersion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gibt an, ob die Benutzer-Metadaten-Datei existiert
        /// </summary>
        bool UserDataFileExists();
    }
}

