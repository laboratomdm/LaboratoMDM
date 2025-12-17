using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models.Policy
{
    public enum PolicyApplicabilityStatus
    {
        Applicable,
        NotApplicable,
        Unknown,
        PolicyNotFound
    }

    public sealed class PolicyApplicabilityResult
    {
        public PolicyApplicabilityStatus Status { get; init; }
        public string Reason { get; init; } = string.Empty;
        public string? Details { get; init; }
    }

}
