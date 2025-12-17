using LaboratoMDM.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Net.NetworkInformation;

namespace LaboratoMDM.ActiveDirectory.Service.Implementations
{
    public class GpoCollector : IGpoCollector
    {
        private readonly ILogger<GpoCollector> _logger;
        private readonly string _domainName;
        private readonly string _domainDn;

        public GpoCollector(ILogger<GpoCollector> logger)
        {
            _logger = logger;

            _domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;

            _domainDn = string.Join(",",
                _domainName.Split('.', StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => $"DC={p}"));
        }

        public GpoTopology? Collect()
        {
            if (string.IsNullOrWhiteSpace(_domainName))
            {
                _logger.LogWarning("Machine is not domain-joined. Skipping GPO collection.");
                return null;
            }

            _logger.LogInformation("Starting GPO topology collection for domain {Domain}", _domainName);

            var topology = new GpoTopology
            {
                Domain = _domainName
            };

            var gpoMap = CollectAllGpos();
            topology.AllGpos.AddRange(gpoMap.Values);

            CollectOuTopology(topology, gpoMap);

            _logger.LogInformation(
                "GPO topology collected. GPOs: {GpoCount}, OUs: {OuCount}",
                topology.AllGpos.Count,
                topology.OuTopology.Count);

            return topology;
        }

        // ===================== GPO =====================
        private Dictionary<string, GpoInfo> CollectAllGpos()
        {
            var result = new Dictionary<string, GpoInfo>(StringComparer.OrdinalIgnoreCase);

            string policiesPath = $"LDAP://CN=Policies,CN=System,{_domainDn}";
            _logger.LogInformation("Collecting GPOs from {Path}", policiesPath);

            using var entry = new DirectoryEntry(policiesPath);
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(objectClass=groupPolicyContainer)"
            };

            searcher.PropertiesToLoad.Add("displayName");
            searcher.PropertiesToLoad.Add("name");
            searcher.PropertiesToLoad.Add("gPCFileSysPath");
            searcher.PropertiesToLoad.Add("flags");
            searcher.PropertiesToLoad.Add("gPCWQLFilter");

            foreach (SearchResult sr in searcher.FindAll())
            {
                string guid = sr.Properties["name"]?[0]?.ToString() ?? "";
                if (string.IsNullOrEmpty(guid))
                    continue;

                int flags = sr.Properties["flags"]?.Count > 0
                    ? Convert.ToInt32(sr.Properties["flags"][0])
                    : 0;

                var gpo = new GpoInfo
                {
                    Guid = guid,
                    DisplayName = sr.Properties["displayName"]?[0]?.ToString() ?? "",
                    FileSysPath = sr.Properties["gPCFileSysPath"]?[0]?.ToString() ?? "",
                    UserEnabled = (flags & 1) == 0,
                    ComputerEnabled = (flags & 2) == 0,
                    WmiFilter = sr.Properties["gPCWQLFilter"]?.Count > 0
                        ? sr.Properties["gPCWQLFilter"][0].ToString()
                        : null
                };

                result[guid] = gpo;

                _logger.LogInformation("GPO: {Name} ({Guid})", gpo.DisplayName, gpo.Guid);
            }

            return result;
        }

        // ===================== OU =====================
        private void CollectOuTopology(GpoTopology topology, Dictionary<string, GpoInfo> gpoMap)
        {
            string domainPath = $"LDAP://{_domainDn}";

            using var entry = new DirectoryEntry(domainPath);
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(objectClass=organizationalUnit)"
            };

            searcher.PropertiesToLoad.Add("name");
            searcher.PropertiesToLoad.Add("distinguishedName");
            searcher.PropertiesToLoad.Add("gPLink");
            searcher.PropertiesToLoad.Add("gPOptions");

            foreach (SearchResult sr in searcher.FindAll())
            {
                var ou = new OuGpoLink
                {
                    OuName = sr.Properties["name"]?[0]?.ToString() ?? "",
                    DistinguishedName = sr.Properties["distinguishedName"]?[0]?.ToString() ?? "",
                    BlockInheritance =
                        sr.Properties["gPOptions"]?.Count > 0 &&
                        Convert.ToInt32(sr.Properties["gPOptions"][0]) == 1
                };

                if (sr.Properties["gPLink"]?.Count > 0)
                {
                    ParseGpLink(sr.Properties["gPLink"][0].ToString(), ou, gpoMap);
                }

                topology.OuTopology.Add(ou);

                _logger.LogInformation(
                    "OU {OU} → GPOs: {Count}, BlockInheritance: {Block}",
                    ou.OuName,
                    ou.GpoLinks.Count,
                    ou.BlockInheritance);
            }
        }

        // ===================== gPLink =====================
        private void ParseGpLink(
            string gpLink,
            OuGpoLink ou,
            Dictionary<string, GpoInfo> gpoMap)
        {
            var links = gpLink.Split(new[] { "][" }, StringSplitOptions.RemoveEmptyEntries);

            int order = 1;

            foreach (var link in links)
            {
                var clean = link.Replace("[", "").Replace("]", "");
                var parts = clean.Split(';');

                if (parts.Length < 2)
                    continue;

                string ldapPath = parts[0];
                int flags = int.Parse(parts[1]);

                string guid = ExtractGuid(ldapPath);
                if (string.IsNullOrEmpty(guid))
                    continue;

                if (!gpoMap.TryGetValue(guid, out var gpo))
                    continue;

                ou.GpoLinks.Add(new GpoLinkInfo
                {
                    Gpo = gpo,
                    LinkOrder = order++,
                    Enabled = (flags & 1) == 0,
                    Enforced = (flags & 2) == 2
                });
            }
        }

        private static string ExtractGuid(string ldapPath)
        {
            int start = ldapPath.IndexOf('{');
            int end = ldapPath.IndexOf('}');
            return (start >= 0 && end > start)
                ? ldapPath.Substring(start + 1, end - start - 1)
                : string.Empty;
        }
    }
}
