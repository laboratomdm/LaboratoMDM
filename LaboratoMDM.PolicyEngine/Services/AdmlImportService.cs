using LaboratoMDM.PolicyEngine.Implementations;
using LaboratoMDM.PolicyEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LaboratoMDM.PolicyEngine.Services;

public class AdmlImportService : IAdmlImportService
{
    private readonly ITranslationService _translationService;
    private readonly ILogger<AdmlImportService> _logger;

    public AdmlImportService(ITranslationService translationService, ILogger<AdmlImportService> logger)
    {
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ImportAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var provider = CreateProviderForFile(filePath);
        _logger.LogInformation("Created translation provider for path: {Path}", filePath);

        var translations = provider.LoadTranslations();
        _logger.LogInformation("Inserting {Count} translations into database", translations.Count());

        // Batch вставка по 1000 записей
        const int batchSize = 1000;
        var batches = translations.Chunk(batchSize);

        foreach (var batch in batches)
        {
            await _translationService.InsertBatchAsync(batch, cancellationToken);
            _logger.LogDebug("Inserted batch of {BatchSize}/{Total}", batch.Length, translations.Count());
        }
    }

    private static AdmlTranslationProvider CreateProviderForFile(string file)
    {
        return new AdmlTranslationProvider(
            file,
            NullLogger<AdmxPolicyProvider>.Instance);
    }
}
