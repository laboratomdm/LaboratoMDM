using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Services
{
    public interface IPolicyMetadataService
    {
        Task<IReadOnlyList<PolicyCategoryEntity>> GetCategoryTree();
        Task<IReadOnlyList<PolicyNamespaceEntity>> GetNamespaces(int admxFileId);
    }
}
