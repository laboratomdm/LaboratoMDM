using LaboratoMDM.Mesh.Master.Models;
using System.Collections.Concurrent;

namespace LaboratoMDM.Mesh.Master.Services
{
    public class AgentRegistry
    {
        private readonly ConcurrentDictionary<string, AgentInfo> _agents = new();

        public void RegisterAgent(AgentInfo agent)
        {
            _agents[agent.AgentId] = agent;
        }

        public void UpdateHeartbeat(string agentId)
        {
            if (_agents.TryGetValue(agentId, out var agent))
            {
                agent.LastHeartbeat = DateTime.UtcNow;
            }
        }

        public IReadOnlyList<AgentInfo> GetAllAgents() => _agents.Values.ToList();

        public AgentInfo? GetAgent(string agentId) => _agents.TryGetValue(agentId, out var agent) ? agent : null;
    }
}
