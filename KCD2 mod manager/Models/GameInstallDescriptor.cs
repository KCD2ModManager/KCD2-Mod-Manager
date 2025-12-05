using KCD2_mod_manager.Services;

namespace KCD2_mod_manager.Models
{
    /// <summary>
    /// Beschreibt eine installierte Spiel-Installation mit allen relevanten Pfaden
    /// </summary>
    public class GameInstallDescriptor
    {
        /// <summary>
        /// Typ des Spiels (KCD1 oder KCD2)
        /// </summary>
        public GameType GameType { get; set; }

        /// <summary>
        /// Root-Pfad der Installation (z.B. ...\KingdomComeDeliverance2\)
        /// </summary>
        public string RootPath { get; set; } = string.Empty;

        /// <summary>
        /// Pfad zum Mods-Ordner (z.B. ...\KingdomComeDeliverance2\Mods)
        /// WICHTIG: Nicht Bin\Mods, sondern direkt im Root!
        /// </summary>
        public string ModsPath { get; set; } = string.Empty;

        /// <summary>
        /// Pfad zum Data-Ordner (z.B. ...\KingdomComeDeliverance2\Data)
        /// </summary>
        public string DataPath { get; set; } = string.Empty;

        /// <summary>
        /// Pfad zum Bin-Ordner (z.B. ...\KingdomComeDeliverance2\Bin\Win64)
        /// </summary>
        public string BinPath { get; set; } = string.Empty;

        /// <summary>
        /// Pfad zur ausführbaren Datei
        /// </summary>
        public string ExecutablePath { get; set; } = string.Empty;

        /// <summary>
        /// Erkannte Spiel-Version
        /// </summary>
        public string? GameVersion { get; set; }
    }
}

