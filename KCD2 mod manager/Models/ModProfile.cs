using System.Collections.Generic;

namespace KCD2_mod_manager.Models
{
    /// <summary>
    /// Repräsentiert ein Mod-Profil (Build-Konfiguration) für verschiedene Playthroughs
    /// </summary>
    public class ModProfile
    {
        /// <summary>
        /// Name des Profils
        /// </summary>
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>
        /// Liste der aktiven Mod-IDs
        /// </summary>
        public List<string> ActiveMods { get; set; } = new();

        /// <summary>
        /// Lade-Reihenfolge der Mods
        /// </summary>
        public List<string> LoadOrder { get; set; } = new();

        /// <summary>
        /// Zeitstempel der letzten Änderung
        /// </summary>
        public string LastModified { get; set; } = System.DateTime.UtcNow.ToString("O");
    }
}

