using LaboratoMDM.Core.Models.Node;

namespace LaboratoMDM.Mesh.Master.Repositories
{
    public interface INodeInfoRepository
    {
        Task UpdateNodeInfo(string agentId, NodeFullInfo info);
        Task<NodeFullInfo?> GetNodeInfo(string agentId);
        Task<IReadOnlyList<NodeFullInfo>> GetAllNodes();
    }
}