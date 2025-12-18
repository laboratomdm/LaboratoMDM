using LaboratoMDM.Core.Models.Node;
using System.Collections.Concurrent;

namespace LaboratoMDM.Mesh.Master.Repositories
{
    public class InMemoryNodeInfoRepository : INodeInfoRepository
    {
        private readonly ConcurrentDictionary<string, NodeFullInfo> _nodes = new();

        public Task UpdateNodeInfo(string agentId, NodeFullInfo info)
        {
            _nodes[agentId] = info;
            return Task.CompletedTask;
        }

        public Task<NodeFullInfo?> GetNodeInfo(string agentId)
        {
            return Task.FromResult(_nodes.TryGetValue(agentId, out var info) ? info : null);
        }

        public Task<IReadOnlyList<NodeFullInfo>> GetAllNodes() => 
            Task.FromResult<IReadOnlyList<NodeFullInfo>>(_nodes.Values.ToList());
    }
}
