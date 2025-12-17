using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine
{
    public interface IPolicyApplicabilityChecker<TPolicy>
    {
        PolicyApplicabilityResult Check(TPolicy policy, PolicyEvaluationContext context);
    }
}
