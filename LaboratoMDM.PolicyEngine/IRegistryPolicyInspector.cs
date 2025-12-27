using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine
{
    public interface IRegistryPolicyInspector
    {
        AgentPolicyReport Inspect(
            PolicyDefinition policy,
            bool expectedEnabled,
            object? expectedValue,
            string? userSid = null);
    }

}
