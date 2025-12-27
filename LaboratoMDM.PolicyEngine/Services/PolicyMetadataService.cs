using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Services.Abstractions;

namespace LaboratoMDM.PolicyEngine.Services
{
    public sealed class PolicyMetadataService : IPolicyMetadataService
    {
        private readonly IPolicyMetadataRepository _repo;

        public PolicyMetadataService(IPolicyMetadataRepository repo)
        {
            _repo = repo;
        }

        public Task<IReadOnlyList<PolicyCategoryEntity>> GetCategoryTree()
            => _repo.GetCategoryTree();

        public Task<IReadOnlyList<PolicyNamespaceEntity>> GetNamespaces(
            int admxFileId)
            => _repo.GetNamespacesForAdmx(admxFileId);
    }
}
