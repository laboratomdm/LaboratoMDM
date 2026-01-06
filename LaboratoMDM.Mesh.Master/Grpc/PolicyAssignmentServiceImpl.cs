using Grpc.Core;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.Mesh.Master.Services;
using LaboratoMDM.Mesh.Master.Services.Abstractions;

namespace LaboratoMDM.Mesh.Master.Grpc;

public sealed class PolicyAssignmentServiceImpl(
    IPolicyAssignService policyAssignmentService,
    IAgentRegistry agentRegistry) : PolicyAssignmentService.PolicyAssignmentServiceBase
{

    /// <summary>
    /// Присвоение политики заданной цели (агент, пользователь или глобально)
    /// </summary>
    public override async Task<AssignPolicyResponse> AssignPolicy(
        AssignPolicyRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.PolicyHash))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "PolicyHash is required"));

        if (request.Target == null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "PolicyTarget is required"));

        // Если цель — агент, проверяем, что агент существует
        if (request.Target.TargetCase == PolicyTarget.TargetOneofCase.Agent)
        {
            var agentId = request.Target.Agent.AgentId;
            var agent = await agentRegistry.GetAgentAsync(agentId);
            if (agent == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Agent {agentId} not found"));
        }

        // Вызов внутреннего сервиса присвоения политики
        await policyAssignmentService.AssignPolicyAsync(
            request.PolicyHash,
            request.Target,
            request.Selection);

        return new AssignPolicyResponse(); // пустой ответ
    }

    /// <summary>
    /// Снятие политики с цели
    /// </summary>
    public override async Task<RemovePolicyResponse> RemovePolicy(
        RemovePolicyRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.PolicyHash))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "PolicyHash is required"));

        if (request.Target == null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "PolicyTarget is required"));

        if (request.Target.TargetCase == PolicyTarget.TargetOneofCase.Agent)
        {
            var agentId = request.Target.Agent.AgentId;
            var agent = await agentRegistry.GetAgentAsync(agentId);
            if (agent == null)
                throw new RpcException(new Status(StatusCode.NotFound, $"Agent {agentId} not found"));
        }

        await policyAssignmentService.RemovePolicyAsync(
            request.PolicyHash,
            request.Target);

        return new RemovePolicyResponse();
    }
}
