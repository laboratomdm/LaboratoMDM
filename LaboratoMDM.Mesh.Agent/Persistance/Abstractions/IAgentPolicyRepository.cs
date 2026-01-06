using LaboratoMDM.Mesh.Agent.Domain;

namespace LaboratoMDM.Mesh.Agent.Persistance.Abstractions
{
    public interface IAgentPolicyRepository
    {
        Task SaveComplianceAsync(AgentPolicyComplianceEntity compliance);
        Task<IReadOnlyList<AgentPolicyComplianceEntity>> GetComplianceForPolicyAsync(string policyHash);
        Task<long> GetLastInstalledRevisionAsync();
    }
}