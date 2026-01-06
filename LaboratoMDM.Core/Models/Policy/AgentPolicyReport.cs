using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models.Policy
{
    public class AgentPolicyReport
    {
        public PolicyComplianceState State { get; set; }
        public string? Key { get; set; }
        public string? ValueName { get; set; }
        public object? ActualValue { get; set; }
        public object? ExpectedValue { get; set; }
        public string? Reason { get; set; }
        public List<AgentPolicyReport> ChildReports { get; set; } = new();
    }

    public enum PolicyComplianceState
    {
        Applied,
        NotApplied,
        Drifted
    }
}