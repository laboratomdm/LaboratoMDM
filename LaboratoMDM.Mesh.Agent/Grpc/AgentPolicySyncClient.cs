#nullable enable
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Laborato.Mesh;
using LaboratoMDM.Mesh.Agent.Services;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.Mesh.Agent.Grpc
{
    /// <summary>
    /// gRPC-клиент для синхронизации политик с мастером.
    /// </summary>
    public sealed class AgentPolicySyncClient
    {
        private readonly GrpcChannel _channel;
        private readonly AgentPolicySyncService _syncService;
        private readonly ILogger<AgentPolicySyncClient> _logger;

        public AgentPolicySyncClient(
            GrpcChannel channel,
            AgentPolicySyncService syncService,
            ILogger<AgentPolicySyncClient> logger)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Синхронизирует локальную базу с мастером.
        /// </summary>
        public async Task SyncAsync(long lastKnownRevision, CancellationToken ct = default)
        {
            _logger.LogInformation("Starting policy sync. Last known revision: {Revision}", lastKnownRevision);

            var client = new PolicySyncService.PolicySyncServiceClient(_channel);

            // Запрашиваем метаданные payload
            var init = await client.StartPolicySyncAsync(new PolicySyncRequest
            {
                AgentId = Environment.MachineName,
                LastKnownRevision = lastKnownRevision
            }, cancellationToken: ct);

            _logger.LogInformation(
                "Payload metadata received: Revision={Revision}, Size={Size} bytes, SHA256={Sha256}",
                init.MasterRevision, init.PayloadSize, init.Sha256);

            // Сохраняем поток payload в временный файл
            var tmpFile = Path.Combine(Path.GetTempPath(), $"agent_payload_{Guid.NewGuid()}.db");
            try
            {
                using var fs = new FileStream(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None);
                using var call = client.StreamPolicyPayload(new Empty(), cancellationToken: ct);

                await foreach (var chunk in call.ResponseStream.ReadAllAsync(ct))
                {
                    await fs.WriteAsync(chunk.Data.ToByteArray(), ct);
                }

                await fs.FlushAsync(ct);
                fs.Close();

                _logger.LogInformation("Payload downloaded to temporary file {TmpFile}", tmpFile);

                // Применяем payload через сервис агента
                await _syncService.ApplyPayloadAsync(tmpFile, init.Sha256, init.MasterRevision, ct);

                _logger.LogInformation("Payload applied successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during policy sync.");
                throw;
            }
            finally
            {
                // временный файл можно удалить, репозиторий сохранил backup
                if (File.Exists(tmpFile))
                    File.Delete(tmpFile);
            }
        }
    }
}
