using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LaboratoMDM.UI.Operator.ViewModels
{
    /// <summary>
    /// ViewModel для одного элемента presentation
    /// </summary>
    public partial class PolicyPresentationElementViewModel : ObservableObject
    {
        public PolicyPresentationElementViewModel()
        {
            Children = new ObservableCollection<PolicyPresentationElementViewModel>();
            Attributes = new Dictionary<string, string>();
            Options = new ObservableCollection<OptionItemViewModel>();
        }

        /// <summary>
        /// Тип элемента: textBox, dropdownList, checkBox, decimalTextBox и т.д.
        /// </summary>
        public string ElementType { get; set; } = "";

        /// <summary>
        /// refId или id элемента
        /// </summary>
        public string ElementId { get; set; } = "";

        /// <summary>
        /// Текст метки или label
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// Значение элемента (для текстовых полей, числовых и чекбоксов)
        /// </summary>
        private object _value;
        public object Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public decimal? Minimum { get; set; }
        public decimal? Maximum { get; set; }
        public decimal? SpinStep { get; set; } = 1;

        /// <summary>
        /// Дочерние элементы (например, label внутри textBox)
        /// </summary>
        public ObservableCollection<PolicyPresentationElementViewModel> Children { get; }

        /// <summary>
        /// Атрибуты элемента (refId, defaultValue, spinStep и т.д.)
        /// </summary>
        public Dictionary<string, string> Attributes { get; }

        /// <summary>
        /// Для dropdownList или listBox: список вариантов
        /// </summary>
        public ObservableCollection<OptionItemViewModel> Options { get; }

        [RelayCommand]
        private void AddOption()
        {
            Options.Add(new OptionItemViewModel());
        }

        [RelayCommand]
        private void RemoveOption(OptionItemViewModel option)
        {
            if (Options.Contains(option))
                Options.Remove(option);
        }

        /// <summary>
        /// Для множественного текста (multiTextBox)
        /// </summary>
        public ObservableCollection<string> MultiTextValues { get; } = new();
    }

    public partial class OptionItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string value = string.Empty;
    }
}
