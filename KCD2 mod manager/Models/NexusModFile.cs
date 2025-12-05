namespace KCD2_mod_manager.Models
{
    /// <summary>
    /// Repräsentiert eine Mod-Datei von Nexus Mods
    /// </summary>
    public class NexusModFile
    {
        public int file_id { get; set; }
        public string version { get; set; } = string.Empty;
        public long uploaded_timestamp { get; set; }
        public string name { get; set; } = string.Empty;
        public string mod_version { get; set; } = string.Empty;
    }

    /// <summary>
    /// Repräsentiert ein Update für eine Mod-Datei
    /// </summary>
    public class NexusModFileUpdate
    {
        public int old_file_id { get; set; }
        public int new_file_id { get; set; }
        public string old_file_name { get; set; } = string.Empty;
        public string new_file_name { get; set; } = string.Empty;
        public long uploaded_timestamp { get; set; }
    }

    /// <summary>
    /// Antwort der Nexus Mods API für Mod-Dateien
    /// </summary>
    public class NexusModFilesResponse
    {
        public List<NexusModFile> files { get; set; } = new();
        public List<NexusModFileUpdate> file_updates { get; set; } = new();
    }
}

