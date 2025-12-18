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

        public static PolicyApplicabilityResult Applicable(string reason) =>
            new()
            {
                Status = PolicyApplicabilityStatus.Applicable,
                Reason = reason
            };

        public static PolicyApplicabilityResult NotApplicable(string reason) =>
            new()
            {
                Status = PolicyApplicabilityStatus.NotApplicable,
                Reason = reason
            };

        public static PolicyApplicabilityResult Unknown(string reason) =>
            new()
            {
                Status = PolicyApplicabilityStatus.Unknown,
                Reason = reason
            };
    }
}
