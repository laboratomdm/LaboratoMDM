using LaboratoMDM.Mesh.Agent.Domain;

namespace LaboratoMDM.Mesh.Agent.Services
{
    public interface IAgentPolicyService
    {
        Task<AgentPolicyEntity?> GetPolicyAsync(string hash);
        Task<IReadOnlyList<AgentPolicyEntity>> GetAllPoliciesAsync();
        Task SaveOrUpdatePolicyAsync(AgentPolicyEntity policy);
        Task SaveComplianceAsync(AgentPolicyComplianceEntity compliance);
        Task<IReadOnlyList<AgentPolicyComplianceEntity>> GetComplianceForPolicyAsync(string policyHash);
    }
}
