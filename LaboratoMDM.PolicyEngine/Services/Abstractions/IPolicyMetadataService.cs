using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Services.Abstractions
{
    public interface IPolicyMetadataService
    {
        Task<IReadOnlyList<PolicyCategoryView>> GetCategoryTree();
        Task<IReadOnlyList<PolicyNamespaceEntity>> GetNamespaces(int admxFileId);
    }
}
