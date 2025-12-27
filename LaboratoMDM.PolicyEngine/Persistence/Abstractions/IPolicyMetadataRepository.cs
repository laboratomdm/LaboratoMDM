using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Persistence.Abstractions
{
    public interface IPolicyMetadataRepository
    {
        // Categories
        Task<PolicyCategoryEntity?> GetCategoryByName(string name);

        Task<PolicyCategoryEntity> CreateCategoryIfNotExists(
            PolicyCategoryEntity category);

        Task CreateCategoriesBatch(IReadOnlyList<PolicyCategoryEntity> categories);

        Task<IReadOnlyList<PolicyCategoryEntity>> GetCategoryTree();

        // Namespaces
        Task<PolicyNamespaceEntity> CreateNamespaceIfNotExists(
            PolicyNamespaceEntity ns);

        Task CreateNamespacesBatch(
            int admxFileId,
            IReadOnlyList<PolicyNamespaceEntity> namespaces);

        Task<IReadOnlyList<PolicyNamespaceEntity>> GetNamespacesForAdmx(
            int admxFileId);
    }
}