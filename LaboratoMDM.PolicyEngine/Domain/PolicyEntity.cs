#nullable enable

using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Domain
{
    /// <summary>
    /// Уникальная политика
    /// </summary>
    public sealed class PolicyEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Указатель на локализованное название политики.
        /// </summary>
        public string? DisplayName { get; init; }

        /// <summary>
        /// Указатель на локализванное описание политики.
        /// </summary>
        public string? ExplainText { get; init; }

        /// <summary>
        /// Scope хранится в базе как строка ("None", "User", "Machine", "Both")
        /// </summary>
        public string ScopeString { get; set; } = "None";

        /// <summary>
        /// Enum-версия Scope для удобного использования в коде
        /// </summary>
        public PolicyScope Scope
        {
            get => ScopeString.ToLowerInvariant() switch
            {
                "user" => PolicyScope.User,
                "machine" => PolicyScope.Machine,
                "both" => PolicyScope.Both,
                _ => PolicyScope.None
            };
            set => ScopeString = value switch
            {
                PolicyScope.User => "User",
                PolicyScope.Machine => "Machine",
                PolicyScope.Both => "Both",
                _ => "None"
            };
        }

        public string RegistryKey { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public int? EnabledValue { get; set; }
        public int? DisabledValue { get; set; }
        public string? SupportedOnRef { get; set; }

        /// <summary>
        /// Хэш политики для уникальности
        /// </summary>
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// Родительская категория (ParentCategory)
        /// </summary>
        public string? ParentCategory { get; set; }

        /// <summary>
        /// Указатель на идентификатор представления политики
        /// </summary>
        public string? PresentationRef { get; set; }

        /// <summary>
        /// Элементы политики (text, checkbox, list и т.д.)
        /// </summary>
        public List<PolicyElementEntity> Elements { get; set; } = new();

        /// <summary>
        /// Зависимости от возможностей ОС
        /// </summary>
        public List<PolicyCapabilityEntity> Capabilities { get; set; } = new();

        /// <summary>
        /// Зависимости от железа
        /// </summary>
        public List<PolicyHardwareRequirementEntity> HardwareRequirements { get; set; } = new();
    }

    /// <summary>
    /// Элемент политики (например текстовое поле, чекбокс, список)
    /// </summary>
    public sealed class PolicyElementEntity
    {
        public int Id { get; set; }
        public int PolicyId { get; set; }

        /// <summary>
        /// Тип элемента: text, decimal, checkbox, list, combobox, dropdownlist, group, etc.
        /// </summary>
        public string Type { get; set; } = "text";

        /// <summary>
        /// Имя/идентификатор элемента
        /// </summary>
        public string IdName { get; set; } = string.Empty;

        /// <summary>
        /// Имя значения в реестре (если применимо)
        /// </summary>
        public string ValueName { get; set; } = string.Empty;

        /// <summary>
        /// Максимальная длина (для text/combobox)
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Обязательность поля
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Client extension для кастомного UI
        /// </summary>
        public string? ClientExtension { get; set; }
    }

    /// <summary>
    /// Зависимость политики от возможностей ОС
    /// </summary>
    public sealed class PolicyCapabilityEntity
    {
        public int Id { get; set; }
        public int PolicyId { get; set; }
        public string Capability { get; set; } = string.Empty;
    }

    /// <summary>
    /// Зависимость политики от железа
    /// </summary>
    public sealed class PolicyHardwareRequirementEntity
    {
        public int Id { get; set; }
        public int PolicyId { get; set; }
        public string HardwareFeature { get; set; } = string.Empty;
    }

    public static class PolicyMapper
    {
        #region PolicyDefinition -> PolicyEntity

        public static PolicyEntity ToEntity(PolicyDefinition def)
        {
            return new PolicyEntity
            {
                Name = def.Name,
                DisplayName = def.DisplayName,
                ExplainText = def.ExplainText,
                Scope = def.Scope,
                RegistryKey = def.RegistryKey,
                ValueName = def.ValueName,
                EnabledValue = def.EnabledValue,
                DisabledValue = def.DisabledValue,
                SupportedOnRef = def.SupportedOnRef,
                ParentCategory = def.ParentCategoryRef,
                PresentationRef = def.PresentationRef,
                Hash = ComputeStableHash(def),
                Elements = def.Elements.Select(ToEntity).ToList(),
                Capabilities = def.RequiredCapabilities.Select(cap => new PolicyCapabilityEntity { Capability = cap }).ToList(),
                HardwareRequirements = def.RequiredHardware.Select(hw => new PolicyHardwareRequirementEntity { HardwareFeature = hw }).ToList()
            };
        }

        private static PolicyElementEntity ToEntity(PolicyElementDefinition def)
        {
            return new PolicyElementEntity
            {
                Type = def.Type,
                IdName = def.IdName,
                ValueName = def.ValueName,
                MaxLength = def.MaxLength,
                Required = def.Required,
                ClientExtension = def.ClientExtension
            };
        }

        private static string ComputeStableHash(PolicyDefinition def)
        {
            var raw =
                $"{def.Name}|{def.Scope}|{def.RegistryKey}|{def.ValueName}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToHexString(
                sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw)));
        }

        #endregion

        #region PolicyEntity -> PolicyDefinition

        public static PolicyDefinition ToDefinition(PolicyEntity entity)
        {
            return new PolicyDefinition
            {
                Name = entity.Name,
                Scope = entity.Scope,
                DisplayName = entity.DisplayName,
                ExplainText = entity.ExplainText,
                RegistryKey = entity.RegistryKey,
                ValueName = entity.ValueName,
                EnabledValue = entity.EnabledValue,
                DisabledValue = entity.DisabledValue,
                SupportedOnRef = entity.SupportedOnRef,
                ParentCategoryRef = entity.ParentCategory,
                PresentationRef = entity.PresentationRef,
                Hash = entity.Hash,
                Elements = entity.Elements.Select(ToDefinition).ToList(),
                RequiredCapabilities = entity.Capabilities.Select(c => c.Capability).ToList(),
                RequiredHardware = entity.HardwareRequirements.Select(h => h.HardwareFeature).ToList()
            };
        }

        private static PolicyElementDefinition ToDefinition(PolicyElementEntity entity)
        {
            return new PolicyElementDefinition
            {
                Type = entity.Type,
                IdName = entity.IdName,
                ValueName = entity.ValueName,
                MaxLength = entity.MaxLength,
                Required = entity.Required,
                ClientExtension = entity.ClientExtension
            };
        }

        #endregion
    }
}