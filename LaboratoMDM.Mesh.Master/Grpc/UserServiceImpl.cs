using Grpc.Core;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.Mesh.Master.Repositories;
using LaboratoMDM.Mesh.Master.Services;

namespace LaboratoMDM.Mesh.Master.Grpc.Services;

public sealed class UserServiceImpl : UserService.UserServiceBase
{
    private readonly INodeInfoRepository _nodeInfoRepository;
    private readonly IAgentRegistry _agentRegistry;

    public UserServiceImpl(
        INodeInfoRepository nodeInfoRepository,
        IAgentRegistry agentRegistry)
    {
        _nodeInfoRepository = nodeInfoRepository;
        _agentRegistry = agentRegistry;
    }

    public override async Task<ListUsersForAgentResponse> ListUsersForAgent(
        ListUsersForAgentRequest request,
        ServerCallContext context)
    {
        var agent = await _agentRegistry.GetAgentAsync(request.AgentId);
        if (agent is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Agent not found"));

        var nodeInfo = await _nodeInfoRepository.GetNodeInfo(request.AgentId);
        if (nodeInfo is null || nodeInfo.Users is null)
            return new ListUsersForAgentResponse();

        var response = new ListUsersForAgentResponse();
        foreach (var user in nodeInfo.Users)
        {
            response.Users.Add(MapUserInfo(user));
        }

        return response;
    }

    private static UserInfo MapUserInfo(Core.Models.User.UserInfo user)
    {
        return new UserInfo
        {
            Name = user.Name,
            Sid = user.Sid,
            AccountType = user.AccountType switch
            {
                Core.Models.User.UserAccountType.Local => UserAccountType.Local,
                Core.Models.User.UserAccountType.System => UserAccountType.System,
                _ => UserAccountType.Unspecified
            },
            IsEnabled = user.IsEnabled,
            LastLogonUnix = ToUnix(user.LastLogon ?? default),
            Groups = { user.Groups ?? Array.Empty<string>() }
        };
    }

    private static long ToUnix(DateTime dateTime)
        => dateTime == default
            ? 0
            : new DateTimeOffset(dateTime).ToUnixTimeSeconds();
}
