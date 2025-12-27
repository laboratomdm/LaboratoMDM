using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.Mesh.Agent.Domain
{
    /// <summary>
    /// Копия политики на агенте (синхронизирована с мастером)
    /// </summary>
    public sealed class AgentPolicyEntity
    {
        public string Hash { get; set; } = string.Empty;  // PRIMARY KEY
        public string Name { get; set; } = string.Empty;
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

        /// <summary>
        /// Ревизия мастера, с которой пришла эта политика
        /// </summary>
        public long SourceRevision { get; set; }

        /// <summary>
        /// Элементы политики (например, текстовое поле, чекбокс и т.д.)
        /// </summary>
        public List<AgentPolicyElementEntity> Elements { get; set; } = new();
    }

    /// <summary>
    /// Элемент политики на агенте
    /// </summary>
    public sealed class AgentPolicyElementEntity
    {
        public int Id { get; set; }
        public string PolicyHash { get; set; } = string.Empty;
        public string ElementId { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string? ValueName { get; set; }
        public int? MaxLength { get; set; }
        public bool Required { get; set; }
        public string? ClientExtension { get; set; }
    }

    /// <summary>
    /// Состояние применения политики на агенте (compliance)
    /// </summary>
    public sealed class AgentPolicyComplianceEntity
    {
        public string PolicyHash { get; set; } = string.Empty;
        public string? UserSid { get; set; } // null для машинного scope
        public string State { get; set; } = "Unknown"; // Applied / NotApplied / Drifted / Unknown
        public string? ActualValue { get; set; } // значение из реестра или системы
        public DateTime LastCheckedAt { get; set; } = DateTime.UtcNow;
    }

    public static class AgentPolicyEntityMapper
    {
        public static AgentPolicyEntity FromPolicyDefinition(
            PolicyDefinition policy,
            long sourceRevision)
        {
            var entity = new AgentPolicyEntity
            {
                Hash = policy.Hash,
                Name = policy.Name,
                Scope = policy.Scope,
                RegistryKey = policy.RegistryKey,
                ValueName = policy.ValueName,
                EnabledValue = policy.EnabledValue,
                DisabledValue = policy.DisabledValue,
                SourceRevision = sourceRevision
            };

            foreach (var element in policy.Elements)
            {
                entity.Elements.Add(new AgentPolicyElementEntity
                {
                    PolicyHash = policy.Hash,
                    ElementId = element.IdName,
                    Type = element.Type,
                    ValueName = element.ValueName,
                    MaxLength = element.MaxLength,
                    Required = element.Required,
                    ClientExtension = element.ClientExtension
                });
            }

            return entity;
        }
    }
}