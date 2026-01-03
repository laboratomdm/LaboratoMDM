using CommunityToolkit.Mvvm.ComponentModel;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using System.Collections.ObjectModel;

namespace LaboratoMDM.UI.Operator.ViewModels
{
    public class PolicyEditorViewModel : ObservableObject
    {
        public ObservableCollection<PolicyPresentationElementViewModel> Elements { get; } = new();

        public void LoadFromPolicyDetails(PolicyDetails policy)
        {
            Elements.Clear();

            if (policy?.PolicyElements == null) return;

            foreach (var pe in policy.Presentation?.Elements ?? [])
            {
                var policyElement = policy.PolicyElements
                    .Where(p => p.ElementId == pe.RefId)
                    .FirstOrDefault();

                var vm = new PolicyPresentationElementViewModel
                {
                    ElementType = pe.Type,
                    ElementId = policyElement?.ElementId ?? string.Empty,
                    Label = pe.Text ?? string.Empty,
                    Value = pe.Type switch
                    {
                        "checkBox" => policyElement?.Required ?? false,
                        "decimalTextBox" => policyElement?.MinValue ?? 0,
                        "textBox" or "multiTextBox" or "dropdownList" or "listBox" => pe.DefaultValue ?? string.Empty,
                        _ => pe.DefaultValue ?? string.Empty
                    },
                    Minimum = policyElement?.MinValue ?? 0,
                    Maximum = policyElement?.MaxValue ?? 100,
                    SpinStep = 1
                };

                if (policyElement?.Items != null)
                {
                    foreach (var item in policyElement?.Items ?? [])
                    {
                        vm.Options.Add(new OptionItemViewModel() { 
                            Value = item.DisplayName ?? item.Name ?? string.Empty
                        });
                    }
                }

                Elements.Add(vm);
            }
        }
    }
}
