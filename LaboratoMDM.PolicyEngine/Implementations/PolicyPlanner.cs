using LaboratoMDM.Core.Models.Policy;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
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

        public PolicyApplicationPlan BuildPlan(PolicyDefinition policy, bool enable)
        {
            _logger.LogInformation(
                "Building plan for policy {Policy} (Enable={Enable})",
                policy.Name, enable);

            var plan = new PolicyApplicationPlan
            {
                PolicyName = policy.Name
            };

            // 1️⃣ List policies
            if (policy.ListKeys.Any())
            {
                foreach (var listKey in policy.ListKeys)
                {
                    plan.Operations.Add(new RegistryOperation
                    {
                        Scope = policy.Scope,
                        Key = listKey,
                        Delete = !enable,
                        Value = enable ? 1 : null,
                        ValueKind = RegistryValueKind.DWord,
                        Reason = "List policy key"
                    });
                }

                _logger.LogDebug("Created {Count} list operations", plan.Operations.Count);
                return plan;
            }

            // 2️⃣ Element-based policies
            if (policy.Elements.Any())
            {
                foreach (var element in policy.Elements)
                {
                    var valueName = element.ValueName ?? policy.ValueName;

                    if (!enable && policy.DisabledValue == null)
                    {
                        plan.Operations.Add(new RegistryOperation
                        {
                            Scope = policy.Scope,
                            Key = policy.RegistryKey,
                            ValueName = valueName,
                            Delete = true,
                            Reason = $"Element {element.IdName}: disabled → delete"
                        });
                        continue;
                    }

                    plan.Operations.Add(new RegistryOperation
                    {
                        Scope = policy.Scope,
                        Key = policy.RegistryKey,
                        ValueName = valueName,
                        ValueKind = RegistryValueKind.DWord,
                        Value = enable
                            ? policy.EnabledValue ?? "1"
                            : policy.DisabledValue,
                        Reason = $"Element {element.IdName}: {(enable ? "enable" : "disable")}"
                    });
                }

                _logger.LogDebug("Created {Count} element operations", plan.Operations.Count);
                return plan;
            }

            // 3️⃣ Legacy single-value policy
            if (!enable && policy.DisabledValue == null)
            {
                plan.Operations.Add(new RegistryOperation
                {
                    Scope = policy.Scope,
                    Key = policy.RegistryKey,
                    ValueName = policy.ValueName,
                    Delete = true,
                    Reason = "Legacy policy: disabled -> delete"
                });

                return plan;
            }

            plan.Operations.Add(new RegistryOperation
            {
                Scope = policy.Scope,
                Key = policy.RegistryKey,
                ValueName = policy.ValueName,
                ValueKind = RegistryValueKind.DWord,
                Value = enable ? policy.EnabledValue ?? "1" : policy.DisabledValue,
                Reason = "Legacy policy: set value"
            });

            return plan;
        }
    }
}