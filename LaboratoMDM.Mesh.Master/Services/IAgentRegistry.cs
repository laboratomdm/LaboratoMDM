using LaboratoMDM.Mesh.Master.Models;

namespace LaboratoMDM.Mesh.Master.Services
{
    public interface IAgentRegistry
    {
        Task RegisterAgentAsync(AgentInfo agent);
        Task UpdateHeartbeatAsync(string agentId);
        Task<AgentInfo?> GetAgentAsync(string agentId);
        Task<IReadOnlyList<AgentInfo>> GetAllAgentsAsync();
        Task<bool> IsAgentAliveAsync(string agentId);
    }
}
