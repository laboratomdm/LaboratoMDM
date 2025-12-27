using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Services.Abstractions
{
    public interface IPolicyMetadataService
    {
        Task<IReadOnlyList<PolicyCategoryEntity>> GetCategoryTree();
        Task<IReadOnlyList<PolicyNamespaceEntity>> GetNamespaces(int admxFileId);
    }
}
