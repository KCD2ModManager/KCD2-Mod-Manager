using System.Collections.Generic;

namespace KCD2_mod_manager.Models
{
    public class ModConflictEntry
    {
        public string ModId { get; set; } = string.Empty;
        public string ModName { get; set; } = string.Empty;
        public bool IsWorkshopMod { get; set; }
    }

    public class ModConflictGroup
    {
        public string FilePath { get; set; } = string.Empty;
        public List<ModConflictEntry> Mods { get; set; } = new();
    }
}
