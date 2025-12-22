using System.Security.Cryptography;
using System.Text;

namespace LaboratoMDM.PolicyEngine.Persistence.Schema
{
    public sealed class MigrationInfo
    {
        public int Version { get; }
        public string Name { get; }
        public string FilePath { get; }
        public string Hash { get; }

        public MigrationInfo(string filePath)
        {
            FilePath = filePath;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var parts = fileName.Split("__", 2);

            if (!int.TryParse(parts[0], out var version))
                throw new InvalidOperationException(
                    $"Invalid migration filename: {fileName}");

            Version = version;
            Name = parts.Length > 1 ? parts[1] : fileName;

            Hash = ComputeHash(File.ReadAllText(filePath));
        }

        private static string ComputeHash(string sql)
        {
            var bytes = Encoding.UTF8.GetBytes(sql);
            return Convert.ToHexString(SHA256.HashData(bytes));
        }
    }
}
