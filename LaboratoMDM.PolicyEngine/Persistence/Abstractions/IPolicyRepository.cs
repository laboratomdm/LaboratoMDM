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
        /// Создаёт уникальную политику, если её ещё нет
        /// </summary>
        Task<PolicyEntity> CreateIfNotExists(PolicyEntity policy);

        /// <summary>
        /// Связывает политику с ADMX файлом
        /// </summary>
        Task LinkPolicyToAdmx(int policyId, int admxFileId);

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
