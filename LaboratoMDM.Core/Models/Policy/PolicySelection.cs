#nullable enable
using System.Collections.Generic;

namespace LaboratoMDM.Core.Models.Policy
{
    /// <summary>
    /// Выбранные значения политики, готовые к применению в реестр
    /// </summary>
    public sealed class PolicySelection
    {
        /// <summary>
        /// Значение политики для одиночного value (legacy)
        /// </summary>
        public string? Value { get; init; }

        /// <summary>
        /// Выбранные значения элементов
        /// </summary>
        public List<PolicyElementSelection> Elements { get; init; } = new();

        /// <summary>
        /// Для List-политик: ключи, которые нужно включить
        /// </summary>
        public List<string> ListKeys { get; init; } = new();
    }

    /// <summary>
    /// Выбранное значение конкретного элемента политики
    /// </summary>
    public sealed class PolicyElementSelection
    {
        public string IdName { get; init; } = string.Empty;

        /// <summary>
        /// Значение элемента (BOOLEAN, DECIMAL, TEXT, MULTITEXT)
        /// </summary>
        public string? Value { get; init; }

        /// <summary>
        /// Выбранные дочерние элементы (для ENUM или LIST)
        /// </summary>
        public List<PolicyElementItemSelection> Childs { get; init; } = new();
    }

    /// <summary>
    /// Выбранное значение дочернего элемента PolicyElement
    /// </summary>
    public sealed class PolicyElementItemSelection
    {
        public string IdName { get; init; } = string.Empty;
        public string? Value { get; init; }

        /// <summary>
        /// Для рекурсивных VALUE_LIST внутри LIST/ENUM
        /// </summary>
        public List<PolicyElementItemSelection> Childs { get; init; } = new();
    }
}
