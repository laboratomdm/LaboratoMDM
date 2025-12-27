using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace LaboratoMDM.UI.Operator.ViewModels
{
    public class PolicyEditorViewModel : ObservableObject
    {
        public ObservableCollection<PolicyPresentationElementViewModel> Elements { get; } = new();

        public void LoadAllPresentationsFromAdml(string admlFilePath)
        {
            //var doc = XDocument.Load(admlFilePath);
            //XNamespace ns = doc.Root.Name.Namespace;
            //var presentationTable = doc.Descendants(ns + "presentationTable").FirstOrDefault();
            //if (presentationTable == null) return;

            Elements.Clear();

            //foreach (var presentation in presentationTable.Elements(ns + "presentation"))
            //{
            //    var vm = new PolicyPresentationElementViewModel
            //    {
            //        ElementType = "presentation",
            //        ElementId = (string)presentation.Attribute("id") ?? "",
            //        Label = "" // presentation сам по себе не имеет текста, только дочерние элементы
            //    };

            //    foreach (var child in presentation.Elements())
            //    {
            //        vm.Children.Add(ParseElement(child));
            //    }

            //    Elements.Add(vm);
            //}


            foreach (var element in CreateSample())
            {
                Elements.Add(element);
            }
        }


        private PolicyPresentationElementViewModel ParseElement(XElement element)
        {
            var vm = new PolicyPresentationElementViewModel
            {
                ElementType = element.Name.LocalName,
                ElementId = (string)element.Attribute("refId") ?? "",
                Label = element.Value.Trim()
            };

            if (element.Name.LocalName == "decimalTextBox")
            {
                if (element.Attribute("spinStep") != null)
                    vm.SpinStep = decimal.Parse(element.Attribute("spinStep")!.Value);

                if (element.Attribute("defaultValue") != null)
                    vm.Value = decimal.Parse(element.Attribute("defaultValue")!.Value);
            }


            foreach (var attr in element.Attributes())
            {
                vm.Attributes[attr.Name.LocalName] = attr.Value;
            }

            foreach (var child in element.Elements())
            {
                vm.Children.Add(ParseElement(child));
            }

            return vm;
        }

        public static ObservableCollection<PolicyPresentationElementViewModel> CreateSample()
        {
            var elements = new ObservableCollection<PolicyPresentationElementViewModel>();

            // Пример decimalTextBox
            elements.Add(new PolicyPresentationElementViewModel
            {
                ElementType = "decimalTextBox",
                ElementId = "AccessRights_RebootTime_seconds",
                Label = "Время (в секундах):",
                Value = 1800,
                Minimum = 0,
                Maximum = 3600,
                SpinStep = 10,
                Attributes = { ["defaultValue"] = "1800", ["spinStep"] = "10" }
            });

            // Пример textBox
            elements.Add(new PolicyPresentationElementViewModel
            {
                ElementType = "textBox",
                ElementId = "RA_Options_Share_Control_Message",
                Label = "Отображать предупреждающее сообщение перед предоставлением общего доступа к управлению:",
                Value = "Предупреждение включено"
            });

            // Пример checkBox
            elements.Add(new PolicyPresentationElementViewModel
            {
                ElementType = "checkBox",
                ElementId = "EnableRemoteControl",
                Label = "Разрешить удаленное управление этим компьютером",
                Value = true
            });

            // Пример dropdownList
            var dropdown = new PolicyPresentationElementViewModel
            {
                ElementType = "dropdownList",
                ElementId = "RA_Solicit_Control_List",
                Label = "Разрешить удаленное управление этим компьютером",
                Value = "Включено"
            };
            dropdown.Options.Add("Включено");
            dropdown.Options.Add("Отключено");
            dropdown.Options.Add("Только чтение");
            elements.Add(dropdown);

            // Пример listBox
            var listBox = new PolicyPresentationElementViewModel
            {
                ElementType = "listBox",
                ElementId = "CustomClasses_List",
                Label = "GUID для нестандартных запоминающих устройств:"
            };
            listBox.Options.Add("{12345678-1234-1234-1234-1234567890AB}");
            listBox.Options.Add("{87654321-4321-4321-4321-BA0987654321}");
            elements.Add(listBox);

            // Пример multiTextBox
            var multiText = new PolicyPresentationElementViewModel
            {
                ElementType = "multiTextBox",
                ElementId = "Notes_Multi",
                Label = "Дополнительные комментарии:"
            };
            multiText.MultiTextValues.Add("Комментарий 1");
            multiText.MultiTextValues.Add("Комментарий 2");
            multiText.MultiTextValues.Add("Комментарий 3");
            elements.Add(multiText);

            // Пример nested presentation с подпунктами
            var nestedPresentation = new PolicyPresentationElementViewModel
            {
                ElementType = "presentation",
                ElementId = "NestedPresentation",
                Label = "Настройки сети"
            };
            nestedPresentation.Children.Add(new PolicyPresentationElementViewModel
            {
                ElementType = "textBox",
                ElementId = "IPAddress",
                Label = "IP-адрес:",
                Value = "192.168.1.100"
            });
            nestedPresentation.Children.Add(new PolicyPresentationElementViewModel
            {
                ElementType = "textBox",
                ElementId = "SubnetMask",
                Label = "Маска подсети:",
                Value = "255.255.255.0"
            });
            elements.Add(nestedPresentation);

            return elements;
        }
    }
}
