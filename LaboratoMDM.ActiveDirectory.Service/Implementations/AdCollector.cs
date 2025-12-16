using LaboratoMDM.Core.Models;
using System.DirectoryServices.ActiveDirectory;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.ActiveDirectory.Service.Implementations
{
    [SupportedOSPlatform("windows")]
    public class AdCollector : IAdCollector
    {
        private readonly ILogger<AdCollector> _logger;

        public AdCollector(ILogger<AdCollector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public DomainInfo? Collect()
        {
            _logger.LogInformation("Starting AD collection.");

            if (!OperatingSystem.IsWindows())
            {
                _logger.LogWarning("AD collection skipped: not running on Windows.");
                return null;
            }

            if (!IsDomainJoined())
            {
                _logger.LogWarning("AD collection skipped: machine is not domain-joined.");
                return null;
            }

            try
            {
                var domain = Domain.GetCurrentDomain();
                var forest = domain.Forest;

                _logger.LogInformation("Connected to domain: {Domain}, forest: {Forest}", domain.Name, forest.Name);

                var info = new DomainInfo
                {
                    DomainName = domain.Name,
                    ForestName = forest.Name
                };

                // Domain Controllers
                foreach (DomainController dc in domain.DomainControllers)
                {
                    info.DomainControllers.Add(dc.Name);
                    _logger.LogInformation("Found DC: {DC}", dc.Name);
                }

                // Определяем DC и PDC роли
                DetermineDcRoles(domain, info);
                _logger.LogInformation("IsDomainController: {IsDC}, IsPdcEmulator: {IsPDC}", info.IsDomainController, info.IsPdcEmulator);

                // Сбор FSMO ролей
                CollectFsmoRoles(domain, forest, info);
                _logger.LogInformation(
                    "FSMO roles collected: RID={Rid}, Infrastructure={Infra}, Schema={Schema}, Naming={Naming}",
                    info.RidMaster, info.InfrastructureMaster, info.SchemaMaster, info.DomainNamingMaster);

                // Сбор репликационных партнёров каждого DC
                CollectReplicationPartners(domain, info);

                // Сбор всех Trusts
                CollectAllTrusts(domain, info);

                // Sites
                foreach (ActiveDirectorySite site in forest.Sites)
                {
                    info.Sites.Add(site.Name);
                    _logger.LogInformation("Found Site: {Site}", site.Name);
                }

                _logger.LogInformation("AD collection completed successfully.");
                return info;
            }
            catch (ActiveDirectoryOperationException ex)
            {
                _logger.LogWarning(ex, "AD collection failed: machine is not associated with a domain or forest.");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "AD collection failed: insufficient privileges.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AD collection failed due to unexpected error.");
                return null;
            }
        }

        private static bool IsDomainJoined()
        {
            try
            {
                string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                return !string.IsNullOrWhiteSpace(domainName);
            }
            catch
            {
                return false;
            }
        }

        private void DetermineDcRoles(Domain domain, DomainInfo info)
        {
            try
            {
                string hostName = Environment.MachineName;

                info.IsDomainController = false;
                info.IsPdcEmulator = false;

                foreach (DomainController dc in domain.DomainControllers)
                {
                    if (dc.Name.StartsWith(hostName, StringComparison.OrdinalIgnoreCase))
                    {
                        info.IsDomainController = true;
                        _logger.LogInformation("This node is a Domain Controller: {DC}", dc.Name);
                        break;
                    }
                }

                if (info.IsDomainController &&
                    domain.PdcRoleOwner.Name.StartsWith(hostName, StringComparison.OrdinalIgnoreCase))
                {
                    info.IsPdcEmulator = true;
                    _logger.LogInformation("This node holds the PDC Emulator role.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to determine DC roles.");
                info.IsDomainController = false;
                info.IsPdcEmulator = false;
            }
        }

        private void CollectFsmoRoles(Domain domain, Forest forest, DomainInfo info)
        {
            try
            {
                // Domain FSMO
                info.RidMaster = domain.RidRoleOwner?.Name;
                info.InfrastructureMaster = domain.InfrastructureRoleOwner?.Name;
                info.IsPdcEmulator = domain.PdcRoleOwner?.Name == Environment.MachineName;

                // Forest FSMO
                info.SchemaMaster = forest.SchemaRoleOwner?.Name;
                info.DomainNamingMaster = forest.NamingRoleOwner?.Name;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect FSMO roles.");
            }
        }

        private void CollectReplicationPartners(Domain domain, DomainInfo info)
        {
            try
            {
                info.DcReplicationPartners = new Dictionary<string, List<string>>();

                foreach (DomainController dc in domain.DomainControllers)
                {
                    try
                    {
                        var partners = new List<string>();
                        foreach (DomainController partner in dc.GetAllReplicationNeighbors())
                        {
                            partners.Add(partner.Name);
                        }

                        info.DcReplicationPartners[dc.Name] = partners;
                        _logger.LogInformation("DC {DC} has {Count} replication partners.", dc.Name, partners.Count);
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogWarning(innerEx, "Failed to get replication partners for DC {DC}", dc.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect replication partners.");
            }
        }

        private void CollectAllTrusts(Domain domain, DomainInfo info)
        {
            try
            {
                foreach (TrustRelationshipInformation trust in domain.GetAllTrustRelationships())
                {
                    string trustInfo = $"{trust.TargetName}:{trust.TrustType}:{trust.TrustDirection}";
                    info.Trusts.Add(trustInfo);
                    _logger.LogInformation("Found Trust: {Trust}", trustInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect domain trusts.");
            }
        }
    }
}