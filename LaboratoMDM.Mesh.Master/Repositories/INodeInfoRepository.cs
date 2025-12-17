using LaboratoMDM.Core.Models.Node;

namespace LaboratoMDM.Mesh.Master.Repositories
{
    public interface INodeInfoRepository
    {
        void UpdateNodeInfo(string agentId, NodeFullInfo info);
        NodeFullInfo? GetNodeInfo(string agentId);
        IReadOnlyList<NodeFullInfo> GetAllNodes();
    }
}