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
    /// Namespace из ADMX файла (target / using)
    /// </summary>
    public sealed class PolicyNamespaceDefinition
    {
        /// <summary>
        /// Префикс (windows, inetres, edge и т.п.)
        /// </summary>
        public string Prefix { get; init; } = string.Empty;

        /// <summary>
        /// Полное пространство имён
        /// </summary>
        public string Namespace { get; init; } = string.Empty;

        /// <summary>
        /// Является ли target namespace (true) или using (false)
        /// </summary>
        public bool IsTarget { get; init; }
    }

    /// <summary>
    /// Категория политик из ADMX (без привязки к БД)
    /// </summary>
    public sealed class PolicyCategoryDefinition
    {
        /// <summary>
        /// Локальное имя категории (без prefix)
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// QName родительской категории (windows:WindowsComponents)
        /// </summary>
        public string? ParentCategoryRef { get; init; }

        /// <summary>
        /// Указатель на идентификатор представления политики
        /// </summary>
        public string? PresentationRef { get; set; }

        /// <summary>
        /// DisplayName (string-id из ADML или raw fallback)
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Explain text (string-id из ADML или raw fallback)
        /// </summary>
        public string? ExplainText { get; init; }
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
        /// Указатель на локализованное название политики.
        /// </summary>
        public string? DisplayName { get; init; }

        /// <summary>
        /// Указатель на локализванное описание политики.
        /// </summary>
        public string? ExplainText { get; init; }

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
        /// Идентификатор представления для данной политики
        /// </summary>
        public string? PresentationRef { get; init; }

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
        /// Элементы политики (text, checkbox, list и т.д.)
        /// </summary>
        public IReadOnlyList<PolicyElementDefinition> Elements { get; init; } = [];

        /// <summary>
        /// Зависимости от возможностей ОС
        /// </summary>
        public IReadOnlyList<string> RequiredCapabilities { get; init; } = [];

        /// <summary>
        /// Зависимости от железа (например TPM, CPU Features, RAM)
        /// </summary>
        public IReadOnlyList<string> RequiredHardware { get; init; } = [];

        /// <summary>
        /// Hash политики для уникальности (может вычисляться через PolicyDefinitionMapper)
        /// </summary>
        public string Hash { get; init; } = string.Empty;
    }

    /// <summary>
    /// Элемент политики (например текстовое поле, чекбокс, список)
    /// </summary>
    public sealed class PolicyElementDefinition
    {
        public string Type { get; init; } = "text";
        public string IdName { get; init; } = string.Empty;
        public string? ValueName { get; init; }
        public int? MaxLength { get; init; }
        public bool Required { get; init; }
        public string? ClientExtension { get; init; }
    }
}
