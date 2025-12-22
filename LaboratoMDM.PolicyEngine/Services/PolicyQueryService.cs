using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;

namespace LaboratoMDM.PolicyEngine.Services
{
    public sealed class PolicyQueryService : IPolicyQueryService
    {
        private readonly IPolicyRepository _repo;

        public PolicyQueryService(IPolicyRepository repo)
        {
            _repo = repo;
        }

        public Task<PolicyEntity?> GetById(int id)
            => _repo.GetById(id);

        public Task<PolicyEntity?> GetByHash(string hash)
            => _repo.GetByHash(hash);

        public Task<IReadOnlyList<PolicyEntity>> GetByCategory(int categoryId)
            => _repo.GetByCategory(categoryId);

        public Task<IReadOnlyList<PolicyEntity>> FindApplicable(
            PolicyEvaluationContext context)
            => _repo.FindApplicablePolicies(context);
    }
}
