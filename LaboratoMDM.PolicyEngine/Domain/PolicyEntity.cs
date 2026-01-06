#nullable enable

using LaboratoMDM.Core.Models.Policy;
using System.Text.Json.Serialization;

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

        public string? EnabledValue { get; set; }
        public string? DisabledValue { get; set; }

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
        /// Указатель на механизм применения политики на машине.
        /// </summary>
        public string? ClientExtension { get; init; }

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

        public string Type { get; set; } = "text";
        public string IdName { get; set; } = string.Empty;

        public string? RegistryKey { get; set; }
        public string? ValueName { get; set; }

        public bool Required { get; set; }
        public int? MaxLength { get; set; }
        public string? ClientExtension { get; set; }

        // list
        public string? ValuePrefix { get; set; }
        public bool? ExplicitValue { get; set; }
        public bool? Additive { get; set; }

        // decimal
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }
        public bool? StoreAsText { get; set; }

        // text / multitext
        public bool? Expandable { get; set; }
        public int? MaxStrings { get; set; }

        public List<PolicyElementItemEntity> Childs { get; set; } = new();
    }


    /// <summary>
    /// Дочерние элементы PolicyElement (напр. итемы elements, enabled/disabled list, enabled/disabled value)
    /// </summary>
    public sealed class PolicyElementItemEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty; // ← вместо IdName
        public int PolicyElementId { get; set; }

        public string ParentTypeString { get; set; } = "elements";
        public string TypeString { get; set; } = "value";
        public string? ValueType { get; set; }

        public string? RegistryKey { get; set; }
        public string? ValueName { get; set; }
        public string? Value { get; set; }

        public string? DisplayName { get; set; }
        public bool? Required { get; set; }

        public int? ParentId { get; set; }
        public List<PolicyElementItemEntity> Childs { get; set; } = new();
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
                ClientExtension = def.ClientExtension,
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
                Type = def.Type.ToString().ToLowerInvariant(),
                IdName = def.IdName,
                ValueName = def.ValueName,
                MaxLength = def.MaxLength,
                Required = def.Required ?? false,
                ClientExtension = def.ClientExtension,

                ValuePrefix = def.ValuePrefix,
                ExplicitValue = def.ExplicitValue,
                Additive = def.Additive,

                MinValue = def.MinValue,
                MaxValue = def.MaxValue,
                StoreAsText = def.StoreAsText,

                Expandable = def.Expandable,
                MaxStrings = def.MaxStrings,

                Childs = def.Childs.Select(ToEntity).ToList()
            };
        }
        private static PolicyElementItemEntity ToEntity(PolicyElementItemDefinition def)
        {
            return new PolicyElementItemEntity
            {
                Name = def.IdName ?? string.Empty,
                ParentTypeString = def.ParentType.ToString().ToLowerInvariant(),
                TypeString = def.Type.ToString().ToLowerInvariant(),
                ValueType = def.ValueType?.ToString().ToLowerInvariant(),
                RegistryKey = def.RegistryKey,
                ValueName = def.ValueName,
                Value = def.Value,
                DisplayName = def.DisplayName,
                Required = def.Required,
                Childs = def.Childs.Select(ToEntity).ToList()
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
                ClientExtension = entity.ClientExtension,
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
                Type = Enum.Parse<PolicyElementType>(
                    entity.Type,
                    ignoreCase: true),

                IdName = entity.IdName,
                RegistryKey = entity.RegistryKey,
                ValueName = entity.ValueName,
                ClientExtension = entity.ClientExtension,

                Required = entity.Required,
                MaxLength = entity.MaxLength,

                // LIST
                ValuePrefix = entity.ValuePrefix,
                ExplicitValue = entity.ExplicitValue,
                Additive = entity.Additive,

                // DECIMAL
                MinValue = entity.MinValue,
                MaxValue = entity.MaxValue,
                StoreAsText = entity.StoreAsText,

                // TEXT / MULTITEXT
                Expandable = entity.Expandable,
                MaxStrings = entity.MaxStrings,

                Childs = entity.Childs.Select(ToDefinition).ToList()
            };
        }
        private static PolicyElementItemDefinition ToDefinition(PolicyElementItemEntity entity)
        {
            return new PolicyElementItemDefinition
            {
                IdName = entity.Name,
                Required = entity.Required,

                ParentType = Enum.Parse<PolicyChildType>(
                    entity.ParentTypeString,
                    ignoreCase: true),

                Type = Enum.Parse<PolicyElementItemType>(
                    entity.TypeString,
                    ignoreCase: true),

                ValueType = entity.ValueType != null
                    ? Enum.Parse<PolicyElementItemValueType>(
                        entity.ValueType,
                        ignoreCase: true)
                    : null,

                RegistryKey = entity.RegistryKey,
                ValueName = entity.ValueName,
                Value = entity.Value,
                DisplayName = entity.DisplayName,

                Childs = entity.Childs.Select(ToDefinition).ToList()
            };
        }

        #endregion
    }
}