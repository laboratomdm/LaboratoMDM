using Microsoft.Extensions.Logging;
using LaboratoMDM.Core.Models;
using LaboratoMDM.ActiveDirectory.Service;
using LaboratoMDM.Core.Services;

namespace LaboratoMDM.Services
{
    public class HybridNodeCollector : IHybridNodeCollector
    {
        private readonly ILogger<HybridNodeCollector> _logger;
        private readonly IAdCollector _adCollector;
        private readonly INodeSystemInfoCollector _systemCollector;

        public HybridNodeCollector(
            ILogger<HybridNodeCollector> logger,
            IAdCollector adCollector,
            INodeSystemInfoCollector systemCollector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _adCollector = adCollector ?? throw new ArgumentNullException(nameof(adCollector));
            _systemCollector = systemCollector ?? throw new ArgumentNullException(nameof(systemCollector));
        }

        /// <summary>
        /// Собирает полный snapshot ноды: системная информация + AD информация (если есть)
        /// </summary>
        public NodeSnapshot Collect()
        {
            _logger.LogInformation("Starting hybrid collection...");

            // Системная информация
            NodeSystemInfo systemInfo = null!;
            try
            {
                _logger.LogInformation("Collecting system hardware info...");
                systemInfo = _systemCollector.Collect();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect system info");
                systemInfo = new NodeSystemInfo();
            }

            // AD информация
            DomainInfo? adInfo = null;
            if (IsDomainJoined())
            {
                try
                {
                    _logger.LogInformation("Node is domain-joined. Collecting AD info...");
                    adInfo = _adCollector.Collect();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect AD info");
                }
            }
            else
            {
                _logger.LogInformation("Node is not domain-joined. Skipping AD info.");
            }

            return new NodeSnapshot
            {
                SystemInfo = systemInfo,
                AdInfo = adInfo
            };
        }

        private static bool IsDomainJoined()
        {
            try
            {
                var domainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
                return !string.IsNullOrWhiteSpace(domainName);
            }
            catch
            {
                return false;
            }
        }
    }

}
