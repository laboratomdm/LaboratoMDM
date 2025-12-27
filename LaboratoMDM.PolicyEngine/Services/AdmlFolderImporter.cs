using LaboratoMDM.PolicyEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LaboratoMDM.PolicyEngine.Services;

public sealed class AdmlFolderImporter : IAdmlFolderImporter
{
    private readonly IAdmlImportService _importService;
    private readonly IAdmlPresentationImportService _presentationImportService;
    private readonly ILogger<AdmlFolderImporter> _logger;

    public AdmlFolderImporter(
        IAdmlImportService importService,
        IAdmlPresentationImportService presentationImportService,
        ILogger<AdmlFolderImporter> logger)
    {
        _importService = importService ??
            throw new ArgumentNullException(nameof(importService));
        _presentationImportService = presentationImportService;
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
    }

    public async Task ImportDirectoryAsync(
        string admlDirectoryPath,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(admlDirectoryPath))
            throw new DirectoryNotFoundException(admlDirectoryPath);

        _logger.LogInformation(
            "Importing ADML directory {Path}",
            admlDirectoryPath);

        foreach (var file in Directory.EnumerateFiles(admlDirectoryPath, "*.adml"))
        {
            await ImportFileAsync(file, ct);
        }
    }

    public async Task ImportFileAsync(
        string admlFilePath,
        CancellationToken ct = default)
    {
        if (!File.Exists(admlFilePath))
            throw new FileNotFoundException(admlFilePath);

        _logger.LogInformation(
            "Importing ADML file {File}",
            admlFilePath);

        await _importService.ImportAsync(admlFilePath, ct);
        _logger.LogInformation("Imported translations from ADML file: {Path}", admlFilePath);

        await _presentationImportService.LoadAndSaveAsync(admlFilePath);
        _logger.LogInformation("Imported presentations from ADML file: {Path}", admlFilePath);
    }

    public async Task ImportDirectoryRecursiveAsync(
        string rootDirectoryPath,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(rootDirectoryPath))
            throw new DirectoryNotFoundException(rootDirectoryPath);

        _logger.LogInformation(
            "Recursively importing ADML directory {Path}",
            rootDirectoryPath);

        // Кэш всех валидных локалей
        var validCultures = new HashSet<string>(
            CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Select(c => c.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var dir in Directory.EnumerateDirectories(rootDirectoryPath))
        {
            var dirName = Path.GetFileName(dir);

            if (!validCultures.Contains(dirName))
            {
                _logger.LogDebug(
                    "Skipping directory {Dir} — not a valid locale",
                    dirName);
                continue;
            }

            _logger.LogInformation(
                "Importing ADML locale directory {Locale}",
                dirName);

            await ImportDirectoryAsync(dir, ct);
        }
    }
}