#nullable enable
using LaboratoMDM.Mesh.Agent.Grpc;
using LaboratoMDM.Mesh.Agent.Persistance;
using LaboratoMDM.Mesh.Agent.Persistance.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.Mesh.Agent.Services
{
    /// <summary>
    /// Hosted service на агенте, который периодически синхронизирует политики с мастером.
    /// </summary>
    public sealed class AgentPolicyHostedService : BackgroundService
    {
        private readonly AgentPolicySyncClient _syncClient;
        private readonly IAgentPolicyRepository _repository;
        private readonly ILogger<AgentPolicyHostedService> _logger;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(30);

        public AgentPolicyHostedService(
            AgentPolicySyncClient syncClient,
            IAgentPolicyRepository repository,
            ILogger<AgentPolicyHostedService> logger)
        {
            _syncClient = syncClient ?? throw new ArgumentNullException(nameof(syncClient));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Метод для ручного запуска синхронизации.
        /// </summary>
        public async Task TrySyncPoliciesAsync(CancellationToken ct = default)
        {
            try
            {
                long lastRevision = await _repository.GetLastInstalledRevisionAsync();
                _logger.LogInformation("Starting manual policy sync. Last known revision: {Revision}", lastRevision);

                await _syncClient.SyncAsync(lastRevision, ct);

                _logger.LogInformation("Manual policy sync completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Manual policy sync failed.");
            }
        }

        /// <summary>
        /// Основной цикл background service.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AgentPolicyHostedService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    long lastRevision = await _repository.GetLastInstalledRevisionAsync();
                    _logger.LogInformation("Starting scheduled policy sync. Last known revision: {Revision}", lastRevision);

                    await _syncClient.SyncAsync(lastRevision, stoppingToken);

                    _logger.LogInformation("Scheduled policy sync completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scheduled policy sync failed.");
                }

                await Task.Delay(_syncInterval, stoppingToken);
            }

            _logger.LogInformation("AgentPolicyHostedService stopping.");
        }
    }
}