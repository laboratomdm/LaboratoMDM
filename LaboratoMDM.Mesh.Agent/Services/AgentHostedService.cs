using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.Agent.Services
{
    public sealed class AgentHostedService : BackgroundService
    {
        private readonly IAgentNodeReporter _reporter;
        private readonly ILogger<AgentHostedService> _logger;

        public AgentHostedService(
            IAgentNodeReporter reporter,
            ILogger<AgentHostedService> logger)
        {
            _reporter = reporter;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Agent started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _reporter.SendOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send node info");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
