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
            var report = new AgentPolicyReport
            {
                State = PolicyComplianceState.Applied,
                ChildReports = new List<AgentPolicyReport>()
            };

            // если элементов нет, fallback на старый одиночный ключ
            if (policy.Elements.Count == 0)
            {
                return InspectRegistryValue(policy.RegistryKey, policy.ValueName, expectedValue, userSid);
            }

            foreach (var element in policy.Elements)
            {
                var elementReport = InspectElement(element, expectedEnabled, userSid);
                report.ChildReports.Add(elementReport);
            }

            // если есть хоть одно несоответствие, меняем общий статус
            report.State = AggregateState(report.ChildReports);

            return report;
        }

        private AgentPolicyReport InspectElement(PolicyElementDefinition element, bool expectedEnabled, string? userSid)
        {
            var report = new AgentPolicyReport
            {
                State = PolicyComplianceState.Applied,
                Key = element.RegistryKey,
                ValueName = element.ValueName,
                ChildReports = new List<AgentPolicyReport>()
            };

            // проверяем сам элемент
            if (!string.IsNullOrEmpty(element.RegistryKey) && !string.IsNullOrEmpty(element.ValueName))
            {
                var expectedValue = element.Value;
                if (!expectedEnabled)
                    expectedValue = element.Value;

                var result = InspectRegistryValue(element.RegistryKey, element.ValueName, expectedValue, userSid);
                report.ChildReports.Add(result);
            }

            // проверяем дочерние элементы
            foreach (var child in element.Childs)
            {
                var childReport = InspectChildItem(child, expectedEnabled, userSid);
                report.ChildReports.Add(childReport);
            }

            report.State = AggregateState(report.ChildReports);
            return report;
        }

        private AgentPolicyReport InspectChildItem(PolicyElementItemDefinition item, bool expectedEnabled, string? userSid)
        {
            var report = new AgentPolicyReport
            {
                State = PolicyComplianceState.Applied,
                Key = item.RegistryKey,
                ValueName = item.ValueName,
                ChildReports = new List<AgentPolicyReport>()
            };

            // для VALUE_LIST или VALUE проверяем сам ключ
            if (!string.IsNullOrEmpty(item.RegistryKey) && !string.IsNullOrEmpty(item.ValueName))
            {
                var expectedValue = item.Value;
                var result = InspectRegistryValue(item.RegistryKey, item.ValueName, expectedValue, userSid);
                report.ChildReports.Add(result);
            }

            // рекурсивно проверяем дочерние элементы
            foreach (var child in item.Childs)
            {
                var childReport = InspectChildItem(child, expectedEnabled, userSid);
                report.ChildReports.Add(childReport);
            }

            report.State = AggregateState(report.ChildReports);
            return report;
        }

        private AgentPolicyReport InspectRegistryValue(string keyPath, string valueName, object? expectedValue, string? userSid)
        {
            var root = ResolveRoot(userSid);

            using var key = root.OpenSubKey(keyPath);
            if (key == null)
                return NotApplied(keyPath, valueName, $"Registry key not found");

            var actual = key.GetValue(valueName);
            if (actual == null)
                return NotApplied(keyPath, valueName, $"Registry value not found");

            if (!Equals(actual, expectedValue))
                return Drifted(keyPath, valueName, actual, expectedValue);

            return Applied(keyPath, valueName, actual);
        }

        private static RegistryKey ResolveRoot(string? sid)
        {
            if (!string.IsNullOrEmpty(sid))
                return Registry.Users.OpenSubKey(sid)!;

            return Registry.CurrentUser;
        }

        private AgentPolicyReport Applied(string key, string valueName, object value) => new()
        {
            State = PolicyComplianceState.Applied,
            Key = key,
            ValueName = valueName,
            ActualValue = value
        };

        private AgentPolicyReport NotApplied(string key, string valueName, string reason) => new()
        {
            State = PolicyComplianceState.NotApplied,
            Key = key,
            ValueName = valueName,
            Reason = reason
        };

        private AgentPolicyReport Drifted(string key, string valueName, object actual, object? expected) => new()
        {
            State = PolicyComplianceState.Drifted,
            Key = key,
            ValueName = valueName,
            ActualValue = actual,
            ExpectedValue = expected
        };

        private static PolicyComplianceState AggregateState(List<AgentPolicyReport> reports)
        {
            if (reports.Any(r => r.State == PolicyComplianceState.Drifted))
                return PolicyComplianceState.Drifted;
            if (reports.Any(r => r.State == PolicyComplianceState.NotApplied))
                return PolicyComplianceState.NotApplied;
            return PolicyComplianceState.Applied;
        }
    }
}
