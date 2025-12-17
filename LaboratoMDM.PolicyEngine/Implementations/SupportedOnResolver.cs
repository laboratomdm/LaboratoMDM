using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    public sealed class SupportedOnResolver : ISupportedOnResolver
    {
        public PolicyApplicabilityResult Resolve(
            string supportedOnRef,
            PolicyEvaluationContext context)
        {
            // Пока — только версия
            // Потом: product, range, and/or/not

            // упрощённо:
            if (supportedOnRef.Contains("Windows10"))
            {
                return context.OsVersion.Major >= 10
                    ? Applicable("Windows 10+")
                    : NotApplicable("Requires Windows 10+");
            }

            return Unknown("Complex supportedOn logic");
        }

        private static PolicyApplicabilityResult Applicable(string reason) =>
            new()
            {
                Status = PolicyApplicabilityStatus.Applicable,
                Reason = reason
            };

        private static PolicyApplicabilityResult NotApplicable(string reason) =>
            new()
            {
                Status = PolicyApplicabilityStatus.NotApplicable,
                Reason = reason
            };

        private static PolicyApplicabilityResult Unknown(string reason) =>
            new()
            {
                Status = PolicyApplicabilityStatus.Unknown,
                Reason = reason
            };
    }

}
