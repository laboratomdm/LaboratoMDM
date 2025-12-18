using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Persistence.Abstractions
{
    public interface IPolicyMetadataRepository
    {
        // Categories
        Task<PolicyCategoryEntity?> GetCategoryByName(string name);

        Task<PolicyCategoryEntity> CreateCategoryIfNotExists(
            PolicyCategoryEntity category);

        Task<IReadOnlyList<PolicyCategoryEntity>> GetCategoryTree();

        // Namespaces
        Task<PolicyNamespaceEntity> CreateNamespaceIfNotExists(
            PolicyNamespaceEntity ns);

        Task<IReadOnlyList<PolicyNamespaceEntity>> GetNamespacesForAdmx(
            int admxFileId);
    }
}
