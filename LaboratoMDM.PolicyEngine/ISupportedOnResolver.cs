using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine
{
    public interface ISupportedOnResolver
    {
        PolicyApplicabilityResult Resolve(string supportedOnRef, PolicyEvaluationContext context);
    }
}
