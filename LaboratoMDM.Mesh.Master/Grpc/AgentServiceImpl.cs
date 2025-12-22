using Grpc.Core;
using LaboratoMDM.Mesh.Master.Services;
using LaboratoMDM.Mesh.Master.Repositories;
using LaboratoMDM.Core.Models.Node;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;

namespace LaboratoMDM.Mesh.Master.Grpc.Services;

public sealed class AgentServiceImpl : AgentService.AgentServiceBase
{
    private readonly IAgentRegistry _agentRegistry;
    private readonly INodeInfoRepository _nodeInfoRepository;

    public AgentServiceImpl(
        IAgentRegistry agentRegistry,
        INodeInfoRepository nodeInfoRepository)
    {
        _agentRegistry = agentRegistry;
        _nodeInfoRepository = nodeInfoRepository;
    }

    public override async Task<ListAgentsResponse> ListAgents(
        ListAgentsRequest request,
        ServerCallContext context)
    {
        var agents = await _agentRegistry.GetAllAgentsAsync();
        var response = new ListAgentsResponse();

        foreach (var agent in agents)
        {
            response.Agents.Add(new AgentSummary
            {
                AgentId = agent.AgentId,
                HostName = agent.HostName,
                IpAddress = agent.IPAddress,
                IsOnline = await _agentRegistry.IsAgentAliveAsync(agent.AgentId),
                LastHeartbeatUnix = ToUnix(agent.LastHeartbeat)
            });
        }

        return response;
    }

    public override async Task<AgentDetails> GetAgent(
        GetAgentRequest request,
        ServerCallContext context)
    {
        var agent = await _agentRegistry.GetAgentAsync(request.AgentId);
        if (agent is null)
            throw new RpcException(
                new Status(StatusCode.NotFound, "Agent not found"));

        var nodeInfo = await _nodeInfoRepository.GetNodeInfo(request.AgentId);
        var isOnline = await _agentRegistry.IsAgentAliveAsync(request.AgentId);

        return new AgentDetails
        {
            AgentId = agent.AgentId,
            HostName = agent.HostName,
            IpAddress = agent.IPAddress,
            IsOnline = isOnline,
            LastHeartbeatUnix = ToUnix(agent.LastHeartbeat),
            NodeInfo = nodeInfo is null
                ? null
                : MapNodeFullInfo(nodeInfo, isOnline)
        };
    }

    private static long ToUnix(DateTime dateTime)
        => dateTime == default
            ? 0
            : new DateTimeOffset(dateTime).ToUnixTimeSeconds();

    private static Grpc.Operator.V1.NodeFullInfo MapNodeFullInfo(
        Core.Models.Node.NodeFullInfo info,
        bool isOnline)
    {
        return new Grpc.Operator.V1.NodeFullInfo
        {
            IsOnline = isOnline,
            LastBootTimeUnix = ToUnix(info.LastBootTime),
            OsBuild = info.OSBuild,
            IsDomainJoined = info.IsDomainJoined,
            AntivirusStatus = info.AntivirusStatus,
            TimeZone = info.TimeZone,
            Manufacturer = info.Manufacturer,
            Model = info.Model,
            FirmwareVersion = info.FirmwareVersion,
            SystemInfo = MapSystemInfo(info.SystemInfo)
        };
    }

    private static Grpc.Operator.V1.NodeSystemInfo MapSystemInfo(
        Core.Models.NodeSystemInfo s)
    {
        var result = new Grpc.Operator.V1.NodeSystemInfo
        {
            HostName = s.HostName,
            OsVersion = s.OSVersion,
            Cpu = s.CPU,
            RamGb = s.RAMGb,
            Motherboard = s.Motherboard
        };

        result.Disks.AddRange(s.Disks);
        result.Gpu.AddRange(s.GPU);
        result.IpAddresses.AddRange(s.IPAddresses);
        result.MacAddresses.AddRange(s.MACAddresses);

        return result;
    }
}
