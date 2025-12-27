namespace LaboratoMDM.PolicyEngine.Domain
{
    /// <summary>
    /// Presentation из ADML (presentationTable/presentation)
    /// </summary>
    public sealed class PresentationEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// presentation id из ADML
        /// </summary>
        public string PresentationId { get; set; } = string.Empty;

        /// <summary>
        /// Имя ADML файла
        /// </summary>
        public string AdmlFile { get; set; } = string.Empty;

        /// <summary>
        /// Элементы управления внутри presentation
        /// </summary>
        public List<PresentationElementEntity> Elements { get; set; } = new();
    }

    /// <summary>
    /// Элемент presentation (dropdownList, textBox, checkBox и т.д.)
    /// </summary>
    public sealed class PresentationElementEntity
    {
        public int Id { get; set; }

        public int PresentationId { get; set; }

        /// <summary>
        /// Тип элемента (строго из ADML)
        /// </summary>
        public string ElementType { get; set; } = string.Empty;

        /// <summary>
        /// refId -> PolicyElements.ElementId (логическая связь)
        /// </summary>
        public string? RefId { get; set; }

        /// <summary>
        /// defaultValue дочернего элемента textBox
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Родительский элемент (ТОЛЬКО для label)
        /// </summary>
        public int? ParentElementId { get; set; }

        /// <summary>
        /// Порядок отображения внутри presentation
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Дочерние элементы (label)
        /// </summary>
        public List<PresentationElementEntity> Children { get; set; } = new();

        /// <summary>
        /// Атрибуты элемента (spinStep, required и т.д.)
        /// </summary>
        public List<PresentationElementAttributeEntity> Attributes { get; set; } = new();

        /// <summary>
        /// Переводы текста элемента (text / label)
        /// </summary>
        public List<PresentationTranslationEntity> Translations { get; set; } = new();
    }

    /// <summary>
    /// Атрибут элемента presentation
    /// </summary>
    public sealed class PresentationElementAttributeEntity
    {
        public int Id { get; set; }
        public int PresentationElementId { get; set; }

        /// <summary>
        /// Имя атрибута (spinStep, required, defaultChecked и т.д.)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Значение атрибута
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Локализованный текст элемента presentation
    /// </summary>
    public sealed class PresentationTranslationEntity
    {
        public int Id { get; set; }

        public int PresentationElementId { get; set; }

        /// <summary>
        /// Код языка (ru-RU, en-US)
        /// </summary>
        public string LangCode { get; set; } = string.Empty;

        /// <summary>
        /// Локализованный текст
        /// </summary>
        public string TextValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Снепшот ADML файла
    /// </summary>
    public sealed class AdmlSnapshot
    {
        public string AdmlFile { get; init; } = string.Empty;

        public IReadOnlyList<PresentationEntity> Presentations { get; init; } = [];
    }

    /// <summary>
    /// Возможные элементы представления
    /// </summary>
    public static class PresentationElementTypes
    {
        public const string DropdownList = "dropdownList";
        public const string Text = "text";
        public const string CheckBox = "checkBox";
        public const string ListBox = "listBox";
        public const string TextBox = "textBox";
        public const string MultiTextBox = "multiTextBox";
        public const string DecimalTextBox = "decimalTextBox";
    }
}
