using LaboratoMDM.Mesh.Agent.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Mesh.Agent.Persistance.Abstractions
{
    public interface IAgentPolicyRepository
    {
        Task<AgentPolicyEntity?> GetPolicyByHashAsync(string hash);
        Task<IReadOnlyList<AgentPolicyEntity>> GetAllPoliciesAsync();
        Task SaveOrUpdatePolicyAsync(AgentPolicyEntity policy);
        Task SaveComplianceAsync(AgentPolicyComplianceEntity compliance);
        Task<IReadOnlyList<AgentPolicyComplianceEntity>> GetComplianceForPolicyAsync(string policyHash);
    }
}
