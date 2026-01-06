using Laborato.Mesh;

namespace LaboratoMDM.Mesh.Agent.Grpc
{
    public interface IAgentPolicyClient
    {
        Task ApplyAsync(string agentAddress, ApplyPolicyRequest request);
        Task RemoveAsync(string agentAddress, RemovePolicyRequest request);
    }
}