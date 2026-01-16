using System;

namespace KCD2_mod_manager.Models
{
    /// <summary>
    /// Benutzerdefinierte Metadaten für einen Mod, die über Manager-Updates hinweg erhalten bleiben
    /// </summary>
    public class UserModData
    {
        /// <summary>
        /// Eindeutige Mod-ID
        /// </summary>
        public string ModId { get; set; } = string.Empty;

        /// <summary>
        /// Vom Benutzer manuell zugewiesene Version (überschreibt die erkannte Version)
        /// </summary>
        public string? CustomVersion { get; set; }

        /// <summary>
        /// Zuletzt erkannte Version aus dem Manifest
        /// </summary>
        public string? LastDetectedVersion { get; set; }

        /// <summary>
        /// Zeitstempel der letzten Aktualisierung (ISO8601)
        /// </summary>
        public string LastUpdated { get; set; } = DateTime.UtcNow.ToString("O");

        /// <summary>
        /// Benutzerdefinierte Notizen für den Mod
        /// </summary>
        public string? CustomNote { get; set; }

        /// <summary>
        /// Benutzerdefinierter Anzeigename für den Mod
        /// </summary>
        public string? CustomName { get; set; }

        /// <summary>
        /// Global zugewiesene Kategorie-ID
        /// </summary>
        public string? CategoryId { get; set; }
    }
}

