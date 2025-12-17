using Grpc.Core;
using Laborato.Mesh;
using Microsoft.Extensions.Logging;
using Google.Protobuf.WellKnownTypes;
using System.Collections.Concurrent;

namespace LaboratoMDM.Mesh.Master
{
    /// <summary>
    /// Реализация MasterService, который принимает NodeFullInfo от агентов.
    /// </summary>
    public class MasterService : MeshService.MeshServiceBase
    {
        private readonly ILogger<MasterService> _logger;

        // Можно хранить состояние агентов (например, последние данные)
        private readonly ConcurrentDictionary<string, NodeFullInfo> _nodes = new();

        public MasterService(ILogger<MasterService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Агент отправляет NodeFullInfo в один запрос (Unary)
        /// </summary>
        public override Task<Empty> SendNodeInfo(NodeFullInfo request, ServerCallContext context)
        {
            if (request?.SystemInfo?.NodeId == null)
            {
                _logger.LogWarning("Received NodeFullInfo with null NodeId");
                return Task.FromResult(new Empty());
            }

            _nodes[request.SystemInfo.NodeId] = request;

            _logger.LogInformation("Received NodeFullInfo from node {NodeId} ({HostName})",
                request.SystemInfo.NodeId, request.SystemInfo.HostName);

            return Task.FromResult(new Empty());
        }

        /// <summary>
        /// Агент может получить команду от мастера (пример запроса)
        /// </summary>
        public override Task<NodeInfoResponse> RequestNodeInfo(NodeInfoRequest request, ServerCallContext context)
        {
            _logger.LogInformation("RequestNodeInfo called by {Caller}", context.Peer);

            // Можно вернуть конкретные данные или просто подтверждение
            return Task.FromResult(new NodeInfoResponse {});
        }

        /// <summary>
        /// Агент потоково отправляет NodeFullInfo (ClientStreaming)
        /// </summary>
        public override async Task<Empty> StreamNodeInfo(IAsyncStreamReader<NodeFullInfo> requestStream, ServerCallContext context)
        {
            await foreach (var nodeInfo in requestStream.ReadAllAsync())
            {
                if (nodeInfo?.SystemInfo?.NodeId != null)
                {
                    _nodes[nodeInfo.SystemInfo.NodeId] = nodeInfo;

                    _logger.LogInformation("Streamed NodeFullInfo from {NodeId} ({HostName})",
                        nodeInfo.SystemInfo.NodeId, nodeInfo.SystemInfo.HostName);
                }
            }

            return new Empty();
        }

        /// <summary>
        /// Получить текущее состояние всех агентов
        /// </summary>
        public IReadOnlyDictionary<string, NodeFullInfo> GetAllNodes() => _nodes;
    }
}
