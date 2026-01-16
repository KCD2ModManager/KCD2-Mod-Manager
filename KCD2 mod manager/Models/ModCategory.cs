using System.Collections.Generic;

namespace KCD2_mod_manager.Models
{
    public class ModCategory
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class CategoryStore
    {
        public int SchemaVersion { get; set; } = 1;
        public List<ModCategory> Categories { get; set; } = new();
    }
}
