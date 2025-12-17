using System.IO.Compression;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    public class ZipArchiver : IArchiver
    {
        public string Archive(string sourcePath, string targetPath)
        {
            if (!Directory.Exists(sourcePath))
                throw new DirectoryNotFoundException($"Папка не найдена: {sourcePath}");

            // Удаляем старый архив если он уже есть
            if (File.Exists(targetPath))
                File.Delete(targetPath);

            ZipFile.CreateFromDirectory(sourcePath, targetPath, CompressionLevel.Optimal, includeBaseDirectory: true);

            return targetPath;
        }
    }
}
