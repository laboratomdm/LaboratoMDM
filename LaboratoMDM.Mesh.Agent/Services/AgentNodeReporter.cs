using Grpc.Net.Client;
using Laborato.Mesh;
using LaboratoMDM.NodeEngine;
using Microsoft.Extensions.Logging;
using static LaboratoMDM.Mesh.Protos.Mapper.NodeFullInfoMapper;

namespace LaboratoMDM.Agent.Services
{
    public sealed class AgentNodeReporter : IAgentNodeReporter
    {
        private readonly INodeFullInfoCollector _collector;
        private readonly MeshService.MeshServiceClient _client;
        private readonly ILogger<AgentNodeReporter> _logger;

        public AgentNodeReporter(
            INodeFullInfoCollector collector,
            GrpcChannel channel,
            ILogger<AgentNodeReporter> logger)
        {
            _collector = collector;
            _client = new MeshService.MeshServiceClient(channel);
            _logger = logger;
        }

        public async Task SendOnceAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Collecting NodeFullInfo...");
            Core.Models.Node.NodeFullInfo info = _collector.Collect();

            var proto = info.ToProto();

            _logger.LogInformation("Sending NodeFullInfo to Master...");
            await _client.SendNodeInfoAsync(proto, cancellationToken: ct);

            _logger.LogInformation("NodeFullInfo sent successfully");
        }

        public async Task StartStreamingAsync(CancellationToken ct = default)
        {
            using var call = _client.StreamNodeInfo(cancellationToken: ct);

            while (!ct.IsCancellationRequested)
            {
                var info = _collector.Collect();
                await call.RequestStream.WriteAsync(info.ToProto());

                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }

            await call.RequestStream.CompleteAsync();
        }
    }
}
