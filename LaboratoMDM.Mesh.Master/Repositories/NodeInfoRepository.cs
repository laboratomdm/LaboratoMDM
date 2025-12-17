using LaboratoMDM.Core.Models.Node;
using System.Collections.Concurrent;

namespace LaboratoMDM.Mesh.Master.Repositories
{
    public class NodeInfoRepository : INodeInfoRepository
    {
        private readonly ConcurrentDictionary<string, NodeFullInfo> _nodes = new();

        public void UpdateNodeInfo(string agentId, NodeFullInfo info)
        {
            _nodes[agentId] = info;
        }

        public NodeFullInfo? GetNodeInfo(string agentId)
        {
            return _nodes.TryGetValue(agentId, out var info) ? info : null;
        }

        public IReadOnlyList<NodeFullInfo> GetAllNodes() => _nodes.Values.ToList();
    }
}
