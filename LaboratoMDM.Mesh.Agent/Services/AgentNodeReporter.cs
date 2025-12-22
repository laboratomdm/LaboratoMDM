using Grpc.Net.Client;
using Laborato.Mesh;
using LaboratoMDM.Mesh.Agent.Options;
using LaboratoMDM.NodeEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LaboratoMDM.Mesh.Protos.Mapper.NodeFullInfoMapper;

namespace LaboratoMDM.Agent.Services
{
    public sealed class AgentNodeReporter : IAgentNodeReporter
    {
        private readonly INodeFullInfoCollector _collector;
        private readonly MeshService.MeshServiceClient _client;
        private readonly IOptions<AgentOptions> _agentOptions;
        private readonly ILogger<AgentNodeReporter> _logger;

        public AgentNodeReporter(
            INodeFullInfoCollector collector,
            GrpcChannel channel,
            IOptions<AgentOptions> agentOptions,
            ILogger<AgentNodeReporter> logger)
        {
            _collector = collector;
            _client = new MeshService.MeshServiceClient(channel);
            _agentOptions = agentOptions;
            _logger = logger;
        }

        public async Task SendOnceAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Collecting NodeFullInfo...");
            Core.Models.Node.NodeFullInfo info = _collector.Collect();
            info.NodeId = _agentOptions.Value.Id;

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
                info.NodeId = _agentOptions.Value.Id;

                await call.RequestStream.WriteAsync(info.ToProto(), ct);

                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }

            await call.RequestStream.CompleteAsync();
        }
    }
}
