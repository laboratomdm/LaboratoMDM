using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Services.Abstractions;

public interface ITranslationService
{
    Task<Translation?> GetByIdAsync(string stringId, string langCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Translation>> GetByLanguageAsync(string langCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Translation>> GetByAdmlFileAsync(string admlFilename, CancellationToken cancellationToken = default);
    Task InsertAsync(Translation translation, CancellationToken cancellationToken = default);
    Task InsertBatchAsync(IEnumerable<Translation> translations, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string stringId, string langCode, CancellationToken cancellationToken = default);
    Task<int> DeleteByStringIdAsync(string stringId, CancellationToken cancellationToken = default);
    Task<int> DeleteByLanguageAsync(string langCode, CancellationToken cancellationToken = default);
    Task<int> CountByLanguageAsync(string langCode, CancellationToken cancellationToken = default);
}