using LaboratoMDM.Core.Models.Policy;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    [SupportedOSPlatform("windows")]
    public sealed class RegistryPolicyApplier : IPolicyApplier
    {
        public void Apply(PolicyApplicationPlan plan)
        {
            foreach (var op in plan.Operations)
            {
                var root = op.Scope == PolicyScope.Machine
                    ? Registry.LocalMachine
                    : Registry.CurrentUser;

                if (op.Delete)
                {
                    root.DeleteSubKeyTree(op.Key, false);
                    continue;
                }

                using var key = root.CreateSubKey(op.Key);
                if (op.Value != null)
                {
                    key?.SetValue(op.ValueName, op.Value, op.ValueKind);
                }
            }
        }
    }
}
