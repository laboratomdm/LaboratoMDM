using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine
{
    public interface IPolicyPlanner
    {
        PolicyApplicationPlan BuildPlan(PolicyDefinition policy, PolicySelection selection);
    }
}