namespace LaboratoMDM.PolicyEngine.Domain
{
    /// <summary>
    /// Загруженный ADMX файл
    /// </summary>
    public sealed class AdmxFileEntity
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileHash { get; set; } = string.Empty;
        public DateTime LoadedAt { get; set; } = DateTime.UtcNow;

        // Связанные политики
        public List<PolicyAdmxMappingEntity> PolicyMappings { get; set; } = new();
    }

    /// <summary>
    /// Связь между политикой и ADMX файлом
    /// </summary>
    public sealed class PolicyAdmxMappingEntity
    {
        public int PolicyId { get; set; }
        public int AdmxFileId { get; set; }
    }

    /// <summary>
    /// Категории политик в конкретном файле
    /// </summary>
    public sealed class PolicyCategoryEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? ExplainText { get; set; }
        public int? ParentCategoryId { get; set; }
        public PolicyCategoryEntity? ParentCategory { get; set; }
        public int AdmxFileId { get; set; }
    }

    /// <summary>
    /// Сокращенные имена пространств имен в ADMX файлах
    /// </summary>
    public sealed class PolicyNamespaceEntity
    {
        public int Id { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public int AdmxFileId { get; set; }
    }

    /// <summary>
    /// Полный снепшот уникального ADMX файла
    /// </summary>
    public sealed class AdmxSnapshot
    {
        public AdmxFileEntity File { get; init; } = null!;
        public IReadOnlyList<PolicyEntity> Policies { get; init; } = Array.Empty<PolicyEntity>();
        public IReadOnlyList<PolicyCategoryEntity> Categories { get; init; } = Array.Empty<PolicyCategoryEntity>();
        public IReadOnlyList<PolicyNamespaceEntity> Namespaces { get; init; } = Array.Empty<PolicyNamespaceEntity>();
    }
}
