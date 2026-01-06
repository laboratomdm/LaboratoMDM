using Laborato.Mesh;

namespace LaboratoMDM.Mesh.Master.Grpc
{
    public interface IAgentPolicyClient
    {
        Task ApplyAsync(string agentAddress, ApplyPolicyRequest request);
        Task RemoveAsync(string agentAddress, RemovePolicyRequest request);
    }
}