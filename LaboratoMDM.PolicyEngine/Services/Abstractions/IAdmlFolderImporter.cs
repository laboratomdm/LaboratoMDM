namespace LaboratoMDM.PolicyEngine.Services.Abstractions;

public interface IAdmlFolderImporter
{
    Task ImportDirectoryAsync(string admlDirectoryPath, CancellationToken ct = default);
    Task ImportFileAsync(string admlFilePath, CancellationToken ct = default);

    /// <summary>
    /// Импортирует все ADML файлы из папки и всех её подпапок
    /// </summary>
    Task ImportDirectoryRecursiveAsync(string rootDirectoryPath, CancellationToken ct = default);
}