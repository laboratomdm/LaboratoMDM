using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.PolicyEngine.Services;

public sealed class TranslationService : ITranslationService
{
    private readonly ITranslationRepository _repository;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(
        ITranslationRepository repository,
        ILogger<TranslationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Translation?> GetByIdAsync(
        string stringId,
        string langCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Getting translation by id {StringId} and language {LangCode}",
            stringId, langCode);

        var result = await _repository.GetByIdAsync(stringId, langCode);

        if (result == null)
        {
            _logger.LogInformation(
                "Translation not found for {StringId} [{LangCode}]",
                stringId, langCode);
        }

        return result;
    }

    public async Task<IReadOnlyList<Translation>> GetByLanguageAsync(
        string langCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Loading translations for language {LangCode}",
            langCode);

        var list = await _repository.GetByLanguageAsync(langCode);

        _logger.LogInformation(
            "Loaded {Count} translations for language {LangCode}",
            list.Count, langCode);

        return list.ToList();
    }

    public async Task<IReadOnlyList<Translation>> GetByAdmlFileAsync(
        string admxFilename,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Loading translations for ADMX file {File}",
            admxFilename);

        var list = await _repository.GetByAdmlFileAsync(admxFilename);

        _logger.LogInformation(
            "Loaded {Count} translations for ADMX file {File}",
            list.Count, admxFilename);

        return list.ToList();
    }

    public async Task InsertAsync(
        Translation translation,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Inserting translation {StringId} [{LangCode}]",
            translation.StringId,
            translation.LangCode);

        var exists = await _repository.ExistsAsync(
            translation.StringId,
            translation.LangCode);

        if (exists)
        {
            _logger.LogWarning(
                "Translation {StringId} [{LangCode}] already exists, replacing",
                translation.StringId,
                translation.LangCode);
        }

        await _repository.InsertAsync(translation);

        _logger.LogInformation(
            "Translation {StringId} [{LangCode}] inserted",
            translation.StringId,
            translation.LangCode);
    }

    public async Task InsertBatchAsync(
        IEnumerable<Translation> translations,
        CancellationToken cancellationToken = default)
    {
        var list = translations.ToList();

        if (!list.Any())
        {
            _logger.LogWarning("InsertBatch called with empty translation list");
            return;
        }

        _logger.LogInformation(
            "Batch inserting {Count} translations",
            list.Count);

        await _repository.InsertBatchAsync(list);

        _logger.LogInformation(
            "Batch insert completed ({Count} translations)",
            list.Count);
    }

    public async Task<bool> ExistsAsync(
        string stringId,
        string langCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Checking existence of translation {StringId} [{LangCode}]",
            stringId, langCode);

        return await _repository.ExistsAsync(stringId, langCode);
    }

    public async Task<int> DeleteByStringIdAsync(
        string stringId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Deleting translations by string id {StringId}",
            stringId);

        var deleted = await _repository.DeleteByStringIdAsync(stringId);

        _logger.LogInformation(
            "Deleted {Count} translations for string id {StringId}",
            deleted, stringId);

        return deleted;
    }

    public async Task<int> DeleteByLanguageAsync(
        string langCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Deleting all translations for language {LangCode}",
            langCode);

        var deleted = await _repository.DeleteByLanguageAsync(langCode);

        _logger.LogInformation(
            "Deleted {Count} translations for language {LangCode}",
            deleted, langCode);

        return deleted;
    }

    public async Task<int> CountByLanguageAsync(
        string langCode,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Counting translations for language {LangCode}",
            langCode);

        var count = await _repository.CountByLanguageAsync(langCode);

        _logger.LogInformation(
            "Language {LangCode} has {Count} translations",
            langCode, count);

        return count;
    }
}