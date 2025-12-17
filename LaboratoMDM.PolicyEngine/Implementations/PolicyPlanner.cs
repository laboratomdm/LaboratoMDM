using LaboratoMDM.Core.Models.Policy;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    [SupportedOSPlatform("windows")]
    public sealed class PolicyPlanner : IPolicyPlanner
    {
        public PolicyApplicationPlan BuildPlan(PolicyDefinition policy, bool enable)
        {
            var plan = new PolicyApplicationPlan
            {
                PolicyName = policy.Name
            };

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
                        ValueKind = RegistryValueKind.DWord
                    });
                }
                return plan;
            }

            if (!enable && policy.DisabledValue == null)
            {
                plan.Operations.Add(new RegistryOperation
                {
                    Scope = policy.Scope,
                    Key = policy.RegistryKey,
                    ValueName = policy.ValueName,
                    Delete = true
                });
                return plan;
            }

            plan.Operations.Add(new RegistryOperation
            {
                Scope = policy.Scope,
                Key = policy.RegistryKey,
                ValueName = policy.ValueName,
                ValueKind = RegistryValueKind.DWord,
                Value = enable ? policy.EnabledValue ?? 1 : policy.DisabledValue
            });

            return plan;
        }
    }
}
