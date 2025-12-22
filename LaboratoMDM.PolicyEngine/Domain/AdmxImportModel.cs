namespace LaboratoMDM.PolicyEngine.Domain
{
    public sealed class AdmxImportModel
    {
        public string FileName { get; init; } = default!;
        public string FileHash { get; init; } = default!;

        public IReadOnlyList<PolicyNamespaceEntity> Namespaces { get; init; } = [];
        public IReadOnlyList<PolicyCategoryEntity> Categories { get; init; } = [];
        public IReadOnlyList<PolicyEntity> Policies { get; init; } = [];
    }

}
