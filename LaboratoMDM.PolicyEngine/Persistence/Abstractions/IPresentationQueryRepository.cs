using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Persistence.Abstractions;

/// <summary>
/// Репозиторий для чтения Presentation и связанных элементов из ADML
/// </summary>
public interface IPresentationQueryRepository
{
    /// <summary>
    /// Загружает полный snapshot всех presentations в конкретном ADML файле
    /// </summary>
    /// <param name="admlFile">Имя ADML файла</param>
    Task<AdmlSnapshot> GetSnapshotAsync(string admlFile);

    /// <summary>
    /// Получает presentation по PresentationId с полным графом элементов
    /// </summary>
    /// <param name="presentationId">Id presentation из ADML</param>
    Task<PresentationEntity?> GetPresentationByIdAsync(string presentationId);

    /// <summary>
    /// Получает все presentation id в конкретном ADML файле
    /// </summary>
    Task<IReadOnlyList<string>> GetPresentationIdsAsync(string admlFile);

    /// <summary>
    /// Проверяет наличие presentation с указанным Id
    /// </summary>
    Task<bool> ExistsAsync(string presentationId, string admlFile);
}
