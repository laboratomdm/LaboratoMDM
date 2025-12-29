using LaboratoMDM.Core.Models.Policy;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Linq;
using System.Runtime.Versioning;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    [SupportedOSPlatform("windows")]
    public sealed class PolicyPlanner : IPolicyPlanner
    {
        private readonly ILogger<PolicyPlanner> _logger;

        public PolicyPlanner(ILogger<PolicyPlanner>? logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PolicyPlanner>.Instance;
        }

        public PolicyApplicationPlan BuildPlan(PolicyDefinition policy, PolicySelection selection)
        {
            var plan = new PolicyApplicationPlan
            {
                PolicyName = policy.Name
            };

            _logger.LogInformation("Building plan for policy {Policy}", policy.Name);

            // List-политики
            foreach (var key in selection.ListKeys)
            {
                plan.Operations.Add(new RegistryOperation
                {
                    Scope = policy.Scope,
                    Key = key,
                    Value = 1,
                    ValueKind = RegistryValueKind.DWord,
                    Delete = false,
                    Reason = "List policy key"
                });
            }

            // Элементы политики
            foreach (var elementSelection in selection.Elements)
            {
                var elementDef = policy.Elements
                    .FirstOrDefault(e => e.IdName == elementSelection.IdName);
                if (elementDef != null)
                    AddElementOperations(plan, policy.Scope, elementDef, elementSelection);
            }

            // Legacy одиночное значение
            if (policy.Elements.Count == 0)
            {
                plan.Operations.Add(new RegistryOperation
                {
                    Scope = policy.Scope,
                    Key = policy.RegistryKey,
                    ValueName = policy.ValueName,
                    Value = selection.Value,
                    ValueKind = RegistryValueKind.DWord,
                    Delete = selection.Value == null,
                    Reason = "Legacy policy"
                });
            }

            return plan;
        }

        private void AddElementOperations(
            PolicyApplicationPlan plan,
            PolicyScope scope,
            PolicyElementDefinition elementDef,
            PolicyElementSelection selection)
        {
            switch (elementDef.Type)
            {
                case PolicyElementType.BOOLEAN:
                case PolicyElementType.DECIMAL:
                case PolicyElementType.TEXT:
                case PolicyElementType.MULTITEXT:
                    if (selection.Value != null)
                    {
                        plan.Operations.Add(new RegistryOperation
                        {
                            Scope = scope,
                            Key = elementDef.RegistryKey ?? string.Empty,
                            ValueName = elementDef.ValueName ?? elementDef.IdName,
                            Value = selection.Value,
                            ValueKind = DetermineRegistryValueKind(elementDef),
                            Delete = false,
                            Reason = $"Element {elementDef.IdName}"
                        });
                    }
                    break;

                case PolicyElementType.ENUM:
                    foreach (var childSelection in selection.Childs)
                    {
                        var childDef = elementDef.Childs
                            .FirstOrDefault(c => c.IdName == childSelection.IdName);
                        if (childDef != null)
                            AddChildItemOperations(plan, scope, childDef, childSelection);
                    }
                    break;

                case PolicyElementType.LIST:
                    foreach (var childSelection in selection.Childs)
                        AddChildItemOperations(plan, scope, null, childSelection, elementDef.RegistryKey);
                    break;
            }
        }

        private void AddChildItemOperations(
            PolicyApplicationPlan plan,
            PolicyScope scope,
            PolicyElementItemDefinition? def,
            PolicyElementItemSelection selection,
            string? parentRegistryKey = null)
        {
            string key = def?.RegistryKey ?? parentRegistryKey ?? string.Empty;
            string valueName = def?.ValueName ?? selection.IdName;

            if (selection.Value != null)
            {
                plan.Operations.Add(new RegistryOperation
                {
                    Scope = scope,
                    Key = key,
                    ValueName = valueName,
                    Value = selection.Value,
                    ValueKind = def != null ? DetermineRegistryValueKind(def) : RegistryValueKind.String,
                    Delete = false,
                    Reason = def != null ? $"Item {def.IdName}" : $"List element {selection.IdName}"
                });
            }

            foreach (var child in selection.Childs)
            {
                PolicyElementItemDefinition? childDef = def?.Childs.FirstOrDefault(c => c.IdName == child.IdName);
                AddChildItemOperations(plan, scope, childDef, child, key);
            }
        }

        private RegistryValueKind DetermineRegistryValueKind(PolicyElementDefinition element)
        {
            return element.Type switch
            {
                PolicyElementType.TEXT => RegistryValueKind.String,
                PolicyElementType.MULTITEXT => RegistryValueKind.MultiString,
                PolicyElementType.DECIMAL => element.StoreAsText == true ? RegistryValueKind.String : RegistryValueKind.DWord,
                PolicyElementType.BOOLEAN => RegistryValueKind.DWord,
                _ => RegistryValueKind.String
            };
        }

        private RegistryValueKind DetermineRegistryValueKind(PolicyElementItemDefinition item)
        {
            return item.ValueType switch
            {
                PolicyElementItemValueType.DECIMAL => RegistryValueKind.DWord,
                PolicyElementItemValueType.STRING => RegistryValueKind.String,
                PolicyElementItemValueType.DELETE => RegistryValueKind.DWord,
                _ => RegistryValueKind.String
            };
        }
    }
}
