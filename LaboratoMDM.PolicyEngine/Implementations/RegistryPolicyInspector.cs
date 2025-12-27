using LaboratoMDM.Core.Models.Policy;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    [SupportedOSPlatform("windows")]
    public sealed class RegistryPolicyInspector : IRegistryPolicyInspector
    {
        private readonly ILogger<RegistryPolicyInspector> _logger;

        public RegistryPolicyInspector(ILogger<RegistryPolicyInspector> logger)
        {
            _logger = logger;
        }

        public AgentPolicyReport Inspect(
            PolicyDefinition policy,
            bool expectedEnabled,
            object? expectedValue,
            string? userSid = null)
        {
            var root = ResolveRoot(policy.Scope, userSid);

            using var key = root.OpenSubKey(policy.RegistryKey);

            if (key == null)
            {
                return NotApplied("Registry key not found");
            }

            var actual = key.GetValue(policy.ValueName);

            if (actual == null)
            {
                return NotApplied("Registry value not found");
            }

            if (!Equals(actual, expectedValue))
            {
                return Drifted(actual, expectedValue);
            }

            return Applied(actual);
        }

        private static RegistryKey ResolveRoot(PolicyScope scope, string? sid)
        {
            if (scope == PolicyScope.Machine)
                return Registry.LocalMachine;

            if (sid == null)
                return Registry.CurrentUser;

            return Registry.Users.OpenSubKey(sid)!;
        }

        private AgentPolicyReport Applied(object value) => new()
        {
            State = PolicyComplianceState.Applied,
            ActualValue = value
        };

        private AgentPolicyReport NotApplied(string reason) => new()
        {
            State = PolicyComplianceState.NotApplied,
            Reason = reason
        };

        private AgentPolicyReport Drifted(object actual, object? expected) => new()
        {
            State = PolicyComplianceState.Drifted,
            ActualValue = actual,
            ExpectedValue = expected
        };
    }

}
