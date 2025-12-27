using Microsoft.Win32;

namespace LaboratoMDM.Core.Models.Policy
{
    public sealed class PolicyApplicationPlan
    {
        public string PolicyName { get; init; } = string.Empty;
        public List<RegistryOperation> Operations { get; } = [];
    }

    public sealed class RegistryOperation
    {
        public PolicyScope Scope { get; init; }
        public string Key { get; init; } = string.Empty;
        public string ValueName { get; init; } = string.Empty;
        public RegistryValueKind ValueKind { get; init; }
        public object? Value { get; init; }
        public bool Delete { get; init; }

        /// <summary>
        /// null = default (machine or current user)
        /// SID = HKU\<SID>
        /// </summary>
        public string? TargetUserSid { get; init; }

        /// <summary>
        /// Для диагностики и тестов
        /// </summary>
        public string Reason { get; init; } = string.Empty;
    }

}
