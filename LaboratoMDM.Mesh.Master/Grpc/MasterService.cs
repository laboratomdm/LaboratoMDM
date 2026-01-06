using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Laborato.Mesh;
using LaboratoMDM.Mesh.Master.Models;
using LaboratoMDM.Mesh.Master.Repositories;
using LaboratoMDM.Mesh.Master.Services;
using LaboratoMDM.Mesh.Protos.Mapper;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.Mesh.Master.Grpc
{
    /// <summary>
    /// MasterService с поддержкой NodeFullInfo и Heartbeat
    /// </summary>
    public sealed class MasterService(
        ILogger<MasterService> logger,
        INodeInfoRepository nodeRepo,
        IAgentRegistry registry) : MeshService.MeshServiceBase
    {
        private readonly ILogger<MasterService> _logger = logger;
        private readonly INodeInfoRepository _nodeRepo = nodeRepo;
        private readonly IAgentRegistry _registry = registry;

        /// <summary>
        /// Агент отправляет NodeFullInfo (Unary)
        /// </summary>
        public override async Task<Empty> SendNodeInfo(NodeFullInfo request, ServerCallContext context)
        {
            var nodeId = request.SystemInfo?.NodeId;
            if (string.IsNullOrWhiteSpace(nodeId))
                return new Empty();

            await _nodeRepo.UpdateNodeInfo(nodeId, request.FromProto());

            await _registry.RegisterAgentAsync(new AgentInfo
            {
                AgentId = nodeId,
                HostName = request.SystemInfo?.HostName ?? throw new Exception("Has no host name"),
                LastHeartbeat = DateTime.UtcNow
            });

            _logger.LogInformation("Node {NodeId} updated", nodeId);
            return new Empty();
        }

        /// <summary>
        /// Запрос команд от мастера.
        /// </summary>
        public override Task<NodeInfoResponse> RequestNodeInfo(NodeInfoRequest request, ServerCallContext context)
        {
            var nodeId = request.NodeId;
            _logger.LogInformation("Command request from node {NodeId}", nodeId);

            return Task.FromResult(new NodeInfoResponse { });
        }

        /// <summary>
        /// Потоковое обновление NodeFullInfo (ClientStreaming)
        /// </summary>
        public override async Task<Empty> StreamNodeInfo(IAsyncStreamReader<NodeFullInfo> requestStream, ServerCallContext context)
        {
            await foreach (var nodeInfo in requestStream.ReadAllAsync())
            {
                var nodeId = nodeInfo.SystemInfo?.NodeId;
                if (string.IsNullOrEmpty(nodeId))
                    continue;

                await _nodeRepo.UpdateNodeInfo(nodeId, nodeInfo.FromProto());
                await _registry.UpdateHeartbeatAsync(nodeId);

                _logger.LogInformation(
                    "Stream update from {NodeId} ({Host})",
                    nodeId,
                    nodeInfo.SystemInfo?.HostName);
            }

            return new Empty();
        }

        /// <summary>
        /// RPC для Heartbeat
        /// </summary>
        public override async Task<HeartbeatResponse> Heartbeat(HeartbeatRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.NodeId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "NodeId is required"));

            await _registry.UpdateHeartbeatAsync(request.NodeId);

            _logger.LogDebug("Heartbeat received from node {NodeId}", request.NodeId);

            return new HeartbeatResponse
            {
                ServerTime = Timestamp.FromDateTime(DateTime.UtcNow.ToUniversalTime())
            };
        }
    }
}