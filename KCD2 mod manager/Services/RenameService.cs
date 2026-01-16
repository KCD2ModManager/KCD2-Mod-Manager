using System.Linq;

namespace KCD2_mod_manager.Services
{
    /// <summary>
    /// Zentraler Service für konsistente Name-Normalisierung
    /// </summary>
    public class RenameService : IRenameService
    {
        private static readonly char[] InvalidNameChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        private readonly IAppSettings _settings;

        public RenameService(IAppSettings settings)
        {
            _settings = settings;
        }

        public string NormalizeName(string input)
        {
            return NormalizeName(input, _settings.EnableFileRenaming);
        }

        public string NormalizeName(string input, bool enableFileRenaming)
        {
            if (!enableFileRenaming || string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }

            var filtered = new string(input.Where(ch => !InvalidNameChars.Contains(ch)).ToArray());
            return filtered;
        }

        public string GenerateModId(string name)
        {
            return GenerateModId(name, _settings.EnableFileRenaming);
        }

        public string GenerateModId(string name, bool enableFileRenaming)
        {
            var normalized = NormalizeName(name ?? string.Empty, enableFileRenaming);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(name ?? string.Empty);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            string fallback = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            if (fallback.Length > 8)
            {
                return fallback.Substring(0, 8);
            }

            return fallback.Length > 0 ? fallback : "moddefault";
        }
    }
}
