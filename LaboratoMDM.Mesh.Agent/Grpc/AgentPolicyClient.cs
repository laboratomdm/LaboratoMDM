using Grpc.Net.Client;
using Laborato.Mesh;

namespace LaboratoMDM.Mesh.Agent.Grpc
{
    public sealed class AgentPolicyClient : IAgentPolicyClient
    {
        public async Task ApplyAsync(string agentAddress, ApplyPolicyRequest request)
        {
            using var channel = GrpcChannel.ForAddress(agentAddress);
            var client = new MeshService.MeshServiceClient(channel);

            var response = await client.ApplyPolicyAsync(request);

            if (!response.Success)
                throw new InvalidOperationException(response.Message);
        }

        public async Task RemoveAsync(string agentAddress, RemovePolicyRequest request)
        {
            using var channel = GrpcChannel.ForAddress(agentAddress);
            var client = new MeshService.MeshServiceClient(channel);

            var response = await client.RemovePolicyAsync(request);

            if (!response.Success)
                throw new InvalidOperationException(response.Message);
        }
    }
}
