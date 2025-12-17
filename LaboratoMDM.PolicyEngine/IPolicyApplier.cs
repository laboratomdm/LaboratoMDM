using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine
{
    public interface IPolicyApplier
    {
        void Apply(PolicyApplicationPlan plan);
    }
}