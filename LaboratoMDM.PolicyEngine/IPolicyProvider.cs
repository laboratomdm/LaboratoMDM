using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine
{
    public interface IPolicyProvider
    {
        IReadOnlyList<PolicyDefinition> LoadPolicies();
        PolicyDefinition? FindPolicy(string name);
    }
}
