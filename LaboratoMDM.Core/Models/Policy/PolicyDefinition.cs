#nullable enable
using System;
using System.Collections.Generic;

namespace LaboratoMDM.Core.Models.Policy
{
    /// <summary>
    /// Область применения политики.
    /// Flags позволяют комбинировать: User, Machine, Both.
    /// </summary>
    [Flags]
    public enum PolicyScope
    {
        None = 0,
        User = 1,
        Machine = 2,
        Both = User | Machine
    }

    ///// <summary>
    ///// Результат применения политики к устройству/контексту.
    ///// </summary>
    //public enum PolicyApplicabilityStatus
    //{
    //    Applicable,
    //    NotApplicable,
    //    Unknown,
    //    PolicyNotFound
    //}

    //public sealed class PolicyApplicabilityResult
    //{
    //    public PolicyApplicabilityStatus Status { get; init; }
    //    public string Reason { get; init; } = string.Empty;
    //    public string? Details { get; init; }
    //}

    /// <summary>
    /// Контекст оценки политики на конкретной машине/пользователе.
    /// </summary>
    public sealed class PolicyEvaluationContext
    {
        public Version OsVersion { get; init; } = Environment.OSVersion.Version;
        public string OsProduct { get; init; } = "Windows";
        // TODO взляд в будущее:
        // public IReadOnlySet<string> InstalledFeatures
        // public HardwareInfo Hardware
        // public IReadOnlySet<string> Capabilities
    }

    /// <summary>
    /// Политика из ADMX/ADML или MDM источника.
    /// </summary>
    public sealed class PolicyDefinition
    {
        /// <summary>
        /// Имя политики, уникальное в пространстве ADMX
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Область применения политики
        /// </summary>
        public PolicyScope Scope { get; init; } = PolicyScope.None;

        /// <summary>
        /// Путь к реестру (если применимо)
        /// </summary>
        public string RegistryKey { get; init; } = string.Empty;

        /// <summary>
        /// Имя значения в реестре (если применимо)
        /// </summary>
        public string ValueName { get; init; } = string.Empty;

        /// <summary>
        /// Значение включено
        /// </summary>
        public int? EnabledValue { get; init; }

        /// <summary>
        /// Значение выключено
        /// </summary>
        public int? DisabledValue { get; init; }

        /// <summary>
        /// Список ключей для List-политик
        /// </summary>
        public IReadOnlyList<string> ListKeys { get; init; } = [];

        /// <summary>
        /// Ссылка на supportedOn (например Windows10)
        /// </summary>
        public string? SupportedOnRef { get; init; }

        /// <summary>
        /// Указатель на родительскую(группирующую) категорию.
        /// </summary>
        public string? ParentCategoryRef { get; init; }

        /// <summary>
        /// Дополнительные зависимости от возможностей системы
        /// </summary>
        public IReadOnlyList<string> RequiredCapabilities { get; init; } = [];

        /// <summary>
        /// Требования к железу (например TPM, CPU Features, RAM)
        /// </summary>
        public IReadOnlyList<string> RequiredHardware { get; init; } = [];
    }
}
