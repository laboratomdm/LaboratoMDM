using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    public sealed class AdmxApplicabilityChecker
    : IPolicyApplicabilityChecker<PolicyDefinition>
    {
        private readonly ISupportedOnResolver _resolver;

        public AdmxApplicabilityChecker(ISupportedOnResolver resolver)
        {
            _resolver = resolver;
        }

        public PolicyApplicabilityResult Check(
            PolicyDefinition policy,
            PolicyEvaluationContext context)
        {
            if (string.IsNullOrEmpty(policy.SupportedOnRef))
            {
                return new PolicyApplicabilityResult
                {
                    Status = PolicyApplicabilityStatus.Applicable,
                    Reason = "No supportedOn restriction"
                };
            }

            return _resolver.Resolve(policy.SupportedOnRef, context);
        }
    }
}
