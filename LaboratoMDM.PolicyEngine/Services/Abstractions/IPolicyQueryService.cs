using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence;

namespace LaboratoMDM.PolicyEngine.Services.Abstractions
{
    public interface IPolicyQueryService
    {
        Task<PolicyEntity?> GetById(int id);
        Task<PolicyEntity?> GetByHash(string hash);
        Task<IReadOnlyList<PolicyEntity>> GetByCategory(int categoryId);
        Task<IReadOnlyList<PolicyEntity>> FindApplicable(
            PolicyEvaluationContext context);

        Task<IReadOnlyList<PolicyGroupView>> GetPoliciesGroupedByScope(string langCode);
        Task<IReadOnlyList<PolicyShortView>> GetShortByCategoryName(string categoryName, string langCode);
        Task<PolicyDetailsView> GetPolicyDetailsView(long id, string langCode);
    }
}
