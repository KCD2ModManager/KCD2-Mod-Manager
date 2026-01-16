namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Zentraler Service für Name-Normalisierung und Mod-ID-Erzeugung
    /// </summary>
    public interface IRenameService
    {
        string NormalizeName(string input, bool enableFileRenaming);
        string NormalizeName(string input);
        string GenerateModId(string name, bool enableFileRenaming);
        string GenerateModId(string name);
    }
}
