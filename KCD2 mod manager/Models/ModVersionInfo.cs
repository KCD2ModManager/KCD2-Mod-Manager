namespace KCD2_mod_manager.Models
{
    /// <summary>
    /// Informationen über die Version eines Mods
    /// </summary>
    public class ModVersionInfo
    {
        public string Version { get; set; } = string.Empty;
        public int ModNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public bool UpdateChecksEnabled { get; set; } = true;
    }
}

