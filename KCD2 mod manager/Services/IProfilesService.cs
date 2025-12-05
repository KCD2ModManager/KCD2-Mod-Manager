using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KCD2_mod_manager.Models;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Interface für die Verwaltung von Mod-Profilen
    /// 
    /// WICHTIG: Profile sind pro Spiel (KCD1/KCD2) getrennt gespeichert.
    /// Jedes Spiel hat sein eigenes Profil-Verzeichnis: Profiles/KCD1/ und Profiles/KCD2/
    /// </summary>
    public interface IProfilesService
    {
        /// <summary>
        /// Lädt alle verfügbaren Profile für ein bestimmtes Spiel
        /// </summary>
        Task<List<ModProfile>> LoadProfilesAsync(GameType gameType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Speichert ein Profil für ein bestimmtes Spiel
        /// </summary>
        Task SaveProfileAsync(GameType gameType, ModProfile profile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Löscht ein Profil für ein bestimmtes Spiel
        /// </summary>
        Task DeleteProfileAsync(GameType gameType, string profileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lädt ein spezifisches Profil für ein bestimmtes Spiel
        /// </summary>
        Task<ModProfile?> LoadProfileAsync(GameType gameType, string profileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Erstellt ein neues Profil basierend auf dem aktuellen Mod-Status
        /// </summary>
        Task<ModProfile> CreateProfileFromCurrentStateAsync(GameType gameType, string profileName, List<Mod> mods, CancellationToken cancellationToken = default);

        /// <summary>
        /// Wendet ein Profil auf die Mod-Liste an
        /// </summary>
        Task ApplyProfileAsync(ModProfile profile, List<Mod> mods, CancellationToken cancellationToken = default);

        /// <summary>
        /// Speichert mod_order.txt für ein Profil (im Profil-Verzeichnis)
        /// </summary>
        Task SaveProfileModOrderAsync(GameType gameType, string profileName, List<Mod> mods, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lädt mod_order.txt für ein Profil (aus dem Profil-Verzeichnis)
        /// </summary>
        Task LoadProfileModOrderAsync(GameType gameType, string profileName, List<Mod> mods, CancellationToken cancellationToken = default);

        /// <summary>
        /// Schreibt mod_order.txt in das Spiel-Mods-Verzeichnis (für das aktive Profil)
        /// </summary>
        Task WriteModOrderToGameFolderAsync(string modFolder, List<Mod> mods, CancellationToken cancellationToken = default);

        /// <summary>
        /// Erstellt ein Standard-Profil, falls noch keines existiert
        /// </summary>
        Task<ModProfile> EnsureDefaultProfileAsync(GameType gameType, List<Mod> mods, CancellationToken cancellationToken = default);
    }
}

