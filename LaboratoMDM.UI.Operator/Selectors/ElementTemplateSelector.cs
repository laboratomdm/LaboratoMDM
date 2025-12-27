using System.Windows;
using System.Windows.Controls;
using LaboratoMDM.UI.Operator.ViewModels;

namespace LaboratoMDM.UI.Operator.Selectors
{
    public class ElementTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextBoxTemplate { get; set; }
        public DataTemplate MultiTextBoxTemplate { get; set; }
        public DataTemplate DropdownTemplate { get; set; }
        public DataTemplate CheckBoxTemplate { get; set; }
        public DataTemplate DecimalTextBoxTemplate { get; set; }
        public DataTemplate LabelTemplate { get; set; }
        public DataTemplate ListBoxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PolicyPresentationElementViewModel vm)
            {
                return vm.ElementType switch
                {
                    "textBox" => TextBoxTemplate,
                    "multiTextBox" => MultiTextBoxTemplate,
                    "dropdownList" => DropdownTemplate,
                    "checkBox" => CheckBoxTemplate,
                    "decimalTextBox" => DecimalTextBoxTemplate,
                    "label" => LabelTemplate,
                    "listBox" => ListBoxTemplate,
                    _ => TextBoxTemplate
                };
            }
            return base.SelectTemplate(item, container);
        }
    }
}
