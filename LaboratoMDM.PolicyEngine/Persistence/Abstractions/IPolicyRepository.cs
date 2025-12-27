using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Persistence.Abstractions
{
    public interface IPolicyRepository
    {
        /// <summary>
        /// Возвращает политику по Id со всеми зависимостями
        /// </summary>
        Task<PolicyEntity?> GetById(int policyId);

        /// <summary>
        /// Возвращает политику по хэшу
        /// </summary>
        Task<PolicyEntity?> GetByHash(string policyHash);

        /// <summary>
        /// Возвращает политики по списку хэшей.
        /// </summary>
        /// <param name="hashes"></param>
        /// <returns></returns>
        Task<IReadOnlyList<PolicyEntity>> GetByHashes(IReadOnlyCollection<string> hashes);

        /// <summary>
        /// Создаёт уникальную политику, если её ещё нет
        /// </summary>
        Task<PolicyEntity> CreateIfNotExists(PolicyEntity policy);

        /// <summary>
        /// Создаёт уникальную политику, если её ещё нет (Режим batch.)
        /// </summary>
        /// <param name="policies"></param>
        /// <returns></returns>
        Task CreatePoliciesBatch(IReadOnlyList<PolicyEntity> policies);

        /// <summary>
        /// Связывает политику с ADMX файлом
        /// </summary>
        Task LinkPolicyToAdmx(int policyId, int admxFileId);

        /// <summary>
        /// Связывает политику с ADMX файлом (РЕЖИМ BATCH)
        /// </summary>
        /// <param name="admxFileId"></param>
        /// <param name="policyIds"></param>
        /// <returns></returns>
        Task LinkPoliciesToAdmxBatch(int admxFileId, IReadOnlyList<int> policyIds);

        /// <summary>
        /// Возвращает все политики,
        /// применимые к данному контексту
        /// </summary>
        Task<IReadOnlyList<PolicyEntity>> FindApplicablePolicies(
            PolicyEvaluationContext context);

        /// <summary>
        /// Возвращает политики по категории (иерархически)
        /// </summary>
        Task<IReadOnlyList<PolicyEntity>> GetByCategory(int categoryId);
    }

}
