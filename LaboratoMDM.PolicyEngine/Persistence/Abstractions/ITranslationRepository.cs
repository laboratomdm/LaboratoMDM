using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Persistence.Abstractions;

public interface ITranslationRepository : IDisposable
{
    Task<Translation?> GetByIdAsync(string stringId, string langCode);
    Task<IReadOnlyList<Translation>> GetByLanguageAsync(string langCode);
    Task<IReadOnlyList<Translation>> GetByAdmlFileAsync(string admlFilename);
    Task InsertAsync(Translation translation);
    Task InsertBatchAsync(IEnumerable<Translation> translations);
    Task<bool> ExistsAsync(string stringId, string langCode);
    Task<int> DeleteByStringIdAsync(string stringId);
    Task<int> DeleteByLanguageAsync(string langCode);
    Task<int> CountByLanguageAsync(string langCode);
}
