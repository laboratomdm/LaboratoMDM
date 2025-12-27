namespace LaboratoMDM.PolicyEngine.Services.Abstractions;
public interface IAdmxFolderImporter
{
    Task ImportFileAsync(
        string admxFilePath,
        CancellationToken ct = default);

    Task ImportDirectoryAsync(
        string admxDirectoryPath,
        CancellationToken ct = default);
}
