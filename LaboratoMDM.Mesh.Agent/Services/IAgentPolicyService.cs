using LaboratoMDM.Mesh.Agent.Domain;

namespace LaboratoMDM.Mesh.Agent.Services
{
    public interface IAgentPolicyService
    {
        Task SaveComplianceAsync(AgentPolicyComplianceEntity compliance);
        Task<IReadOnlyList<AgentPolicyComplianceEntity>> GetComplianceForPolicyAsync(string policyHash);
    }
}
