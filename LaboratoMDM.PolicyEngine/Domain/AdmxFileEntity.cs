using LaboratoMDM.Core.Models.Policy;

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
        public string? ParentCategoryRef { get; set; }
        public PolicyCategoryEntity? ParentCategory { get; set; }
        public int AdmxFileId { get; set; }
    }

    /// <summary>
    /// Сокращенные имена пространств имен в ADMX файлах
    /// </summary>
    public sealed class PolicyNamespaceEntity
    {
        public int? Id { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public int? AdmxFileId { get; set; }
    }

    /// <summary>
    /// Полный снепшот уникального ADMX файла
    /// </summary>
    public sealed class AdmxSnapshot
    {
        public AdmxFileEntity File { get; init; } = null!;
        public IReadOnlyList<PolicyEntity> Policies { get; init; } = [];
        public IReadOnlyList<PolicyCategoryEntity> Categories { get; init; } = [];
        public IReadOnlyList<PolicyNamespaceEntity> Namespaces { get; init; } = [];
        public Dictionary<string, SupportedOnDefinition> SupportedOnDefinitions { get; init; } = [];
    }

    public static class PolicyCategoryMapper
    {
        #region PolicyCategoryDefinition -> PolicyCategoryEntity

        public static PolicyCategoryEntity ToEntity(
            PolicyCategoryDefinition def,
            int admxFileId)
        {
            return new PolicyCategoryEntity
            {
                Name = def.Name,
                DisplayName = def.DisplayName,
                ExplainText = def.ExplainText,
                ParentCategoryRef = def.ParentCategoryRef,
                AdmxFileId = admxFileId
            };
        }

        #endregion

        #region PolicyCategoryEntity -> PolicyCategoryDefinition

        public static PolicyCategoryDefinition ToDefinition(
            PolicyCategoryEntity entity)
        {
            return new PolicyCategoryDefinition
            {
                Name = entity.Name,
                DisplayName = entity.DisplayName,
                ExplainText = entity.ExplainText,
                ParentCategoryRef = entity.ParentCategoryRef
            };
        }

        #endregion
    }

    public static class PolicyNamespaceMapper
    {
        #region PolicyNamespaceDefinition -> PolicyNamespaceEntity

        public static PolicyNamespaceEntity ToEntity(PolicyNamespaceDefinition def)
        {
            return new PolicyNamespaceEntity
            {
                Prefix = def.Prefix,
                Namespace = def.Namespace
            };
        }

        #endregion

        #region PolicyNamespaceEntity -> PolicyNamespaceDefinition

        public static PolicyNamespaceDefinition ToDefinition(
            PolicyNamespaceEntity entity)
        {
            return new PolicyNamespaceDefinition
            {
                Prefix = entity.Prefix,
                Namespace = entity.Namespace,
                IsTarget = true
            };
        }

        #endregion
    }

}
