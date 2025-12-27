using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models.Policy
{
    public enum PolicyComplianceState
    {
        NotApplied = 0,
        Applied = 1,
        Drifted = 2
    }

    public sealed class AgentPolicyReport
    {
        public string PolicyHash { get; init; } = string.Empty;
        public PolicyScope Scope { get; init; }

        public string? UserSid { get; init; }

        public PolicyComplianceState State { get; init; }

        public object? ActualValue { get; init; }
        public object? ExpectedValue { get; init; }

        public string? Reason { get; init; }
    }

}
