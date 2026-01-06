using Laborato.Mesh;
using LaboratoMDM.Mesh.Master.Grpc;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.Mesh.Master.Models;
using LaboratoMDM.Mesh.Master.Services.Abstractions;
using LaboratoMDM.PolicyEngine.Services.Abstractions;
using StackExchange.Redis;
using System.Text.Json;

namespace LaboratoMDM.Mesh.Master.Services;

public sealed class PolicyAssignService : IPolicyAssignService
{
    private readonly IPolicyQueryService _policyQueryService;
    private readonly IAgentRegistry _agentRegistry;
    private readonly IAgentPolicyClient _agentPolicyClient;
    private readonly IDatabase _db;

    private const string AssignmentPrefix = "policy_assignment:";

    public PolicyAssignService(
        IPolicyQueryService policyQueryService,
        IAgentRegistry agentRegistry,
        IAgentPolicyClient agentPolicyClient,
        IConnectionMultiplexer redis)
    {
        _policyQueryService = policyQueryService;
        _agentRegistry = agentRegistry;
        _agentPolicyClient = agentPolicyClient;
        _db = redis.GetDatabase();
    }

    public async Task AssignPolicyAsync(string policyHash, PolicyTarget target, Grpc.Operator.V1.PolicySelection selection)
    {
        // Проверяем, что политика существует на мастере
        var policy = await _policyQueryService.GetByHash(policyHash);
        if (policy == null)
            throw new InvalidOperationException($"Policy {policyHash} not found on master");

        var pSelection = MapSelection(selection);
        // Получаем адрес агента
        string ? agentAddress = null;

        switch (target.TargetCase)
        {
            case PolicyTarget.TargetOneofCase.Agent:
                var agentId = target.Agent.AgentId;
                var agent = await _agentRegistry.GetAgentAsync(agentId);
                if (agent == null)
                    throw new InvalidOperationException($"Agent {agentId} not found");

                agentAddress = agent.GrpcAddress; // предполагаем, что есть GrpcAddress
                break;

            case PolicyTarget.TargetOneofCase.Global:
                // глобальная политика может применяться сразу к списку всех агентов
                var agents = await _agentRegistry.GetAllAgentsAsync();
                foreach (var a in agents)
                {
                    await ApplyPolicyToAgent(a, policy, pSelection, target);
                }
                return;

            case PolicyTarget.TargetOneofCase.User:
                agentAddress = (await _agentRegistry.GetAgentAsync(target.User.AgentId))?.GrpcAddress;
                if (agentAddress == null)
                    throw new InvalidOperationException($"Agent {target.User.AgentId} not found");
                break;

            default:
                throw new InvalidOperationException("Unsupported target type");
        }
        var applyAgent = await _agentRegistry.GetAgentAsync(target.User.AgentId)!;
        if (agentAddress != null)
            await ApplyPolicyToAgent(applyAgent, policy, pSelection, target);
    }

    public async Task RemovePolicyAsync(string policyHash, PolicyTarget target)
    {
        var policy = await _policyQueryService.GetByHash(policyHash);
        if (policy == null)
            throw new InvalidOperationException($"Policy {policyHash} not found on master");

        string? agentAddress = null;

        switch (target.TargetCase)
        {
            case PolicyTarget.TargetOneofCase.Agent:
                var agentId = target.Agent.AgentId;
                var agent = await _agentRegistry.GetAgentAsync(agentId);
                if (agent == null)
                    throw new InvalidOperationException($"Agent {agentId} not found");

                agentAddress = agent.GrpcAddress;
                break;

            case PolicyTarget.TargetOneofCase.Global:
                var agents = await _agentRegistry.GetAllAgentsAsync();
                foreach (var a in agents)
                {
                    await RemovePolicyFromAgent(a, policy, target);
                }
                return;

            case PolicyTarget.TargetOneofCase.User:
                agentAddress = (await _agentRegistry.GetAgentAsync(target.User.AgentId))?.GrpcAddress;
                if (agentAddress == null)
                    throw new InvalidOperationException($"Agent {target.User.AgentId} not found");
                break;

            default:
                throw new InvalidOperationException("Unsupported target type");
        }
        var applyAgent = await _agentRegistry.GetAgentAsync(target.User.AgentId)!;
        if (agentAddress != null)
            await RemovePolicyFromAgent(applyAgent, policy, target);
    }

    // Внутренние методы для работы с агентом
    private async Task ApplyPolicyToAgent(AgentInfo agent, LaboratoMDM.PolicyEngine.Domain.PolicyEntity policy,
        Laborato.Mesh.PolicySelection selection, PolicyTarget target)
    {
        var request = new ApplyPolicyRequest
        {
            PolicyHash = policy.Hash,
            Revision = 1, //todo change to actual revision
            Enable = true,
            Selection = selection
        };

        switch (target.TargetCase)
        {
            case PolicyTarget.TargetOneofCase.User:
                request.UserSid = target.User.UserSid;
                break;
            case PolicyTarget.TargetOneofCase.Agent:
                request.MachineScope = true;
                break;
            case PolicyTarget.TargetOneofCase.Global:
                request.MachineScope = true;
                break;
        }

        await _agentPolicyClient.ApplyAsync(agent.GrpcAddress, request);

        // сохраняем назначение в Redis
        var key = AssignmentKey(policy.Hash, agent.AgentId, target);
        await _db.StringSetAsync(key, JsonSerializer.Serialize(selection));
    }

    private async Task RemovePolicyFromAgent(AgentInfo agent, LaboratoMDM.PolicyEngine.Domain.PolicyEntity policy,
        PolicyTarget target)
    {
        var request = new Laborato.Mesh.RemovePolicyRequest
        {
            PolicyHash = policy.Hash,
            Revision = 1//policy.Revision
        };

        switch (target.TargetCase)
        {
            case PolicyTarget.TargetOneofCase.User:
                request.UserSid = target.User.UserSid;
                break;
            case PolicyTarget.TargetOneofCase.Agent:
                request.MachineScope = true;
                break;
            case PolicyTarget.TargetOneofCase.Global:
                request.MachineScope = true;
                break;
        }

        await _agentPolicyClient.RemoveAsync(agent.GrpcAddress, request);

        // удаляем назначение из Redis
        var key = AssignmentKey(policy.Hash, agent.AgentId, target);
        await _db.KeyDeleteAsync(key);
    }

    private static string AssignmentKey(string policyHash, string agentId, PolicyTarget target)
        => $"{AssignmentPrefix}{policyHash}:{agentId}:{target.TargetCase}";

    private static Laborato.Mesh.PolicySelection MapSelection(Grpc.Operator.V1.PolicySelection selection)
    {
        if (selection == null)
            return new Laborato.Mesh.PolicySelection();

        var result = new Laborato.Mesh.PolicySelection
        {
            Value = selection.Value
        };

        result.ListKeys.AddRange(selection.ListKeys);

        foreach (var element in selection.Elements)
        {
            result.Elements.Add(MapElement(element));
        }

        return result;
    }

    private static Laborato.Mesh.PolicyElementSelection MapElement(Grpc.Operator.V1.PolicyElementSelection element)
    {
        var mapped = new Laborato.Mesh.PolicyElementSelection
        {
            IdName = element.IdName,
            Value = element.Value
        };

        foreach (var child in element.Childs)
        {
            mapped.Childs.Add(MapItem(child));
        }

        return mapped;
    }

    private static Laborato.Mesh.PolicyElementItemSelection MapItem(Grpc.Operator.V1.PolicyElementItemSelection item)
    {
        var mapped = new Laborato.Mesh.PolicyElementItemSelection
        {
            IdName = item.IdName,
            Value = item.Value
        };

        foreach (var child in item.Childs)
        {
            mapped.Childs.Add(MapItem(child));
        }

        return mapped;
    }

}
