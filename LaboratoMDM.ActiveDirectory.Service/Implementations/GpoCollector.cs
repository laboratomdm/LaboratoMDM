using LaboratoMDM.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Net.NetworkInformation;

namespace LaboratoMDM.ActiveDirectory.Service.Implementations
{
    public class GpoCollector : IGpoCollector
    {
        private readonly ILogger<GpoCollector> _logger;
        private readonly string _domainDn;
        private readonly string _domainName;

        public GpoCollector(ILogger<GpoCollector> logger)
        {
            _logger = logger;

            _domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            _domainDn = string.Join(",",
                _domainName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                           .Select(p => $"DC={p}"));
        }

        public GpoTreeInfo? Collect()
        {
            if (string.IsNullOrWhiteSpace(_domainName))
            {
                _logger.LogWarning("Machine is not domain-joined. Skipping GPO collection.");
                return null;
            }

            _logger.LogInformation("Starting GPO collection for domain {Domain}", _domainName);

            var result = new GpoTreeInfo
            {
                Domain = _domainName
            };

            // 1. Собираем ВСЕ GPO
            var gpoMap = CollectAllGpos();
            result.AllGpos.AddRange(gpoMap.Values);

            // 2. Собираем OU → GPO links
            CollectOuLinks(result, gpoMap);

            _logger.LogInformation(
                "GPO collection completed. Total GPOs: {GpoCount}, OUs: {OuCount}",
                result.AllGpos.Count,
                result.OuLinks.Count);

            return result;
        }

        private Dictionary<string, GpoInfo> CollectAllGpos()
        {
            var gpos = new Dictionary<string, GpoInfo>(StringComparer.OrdinalIgnoreCase);

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

            foreach (SearchResult sr in searcher.FindAll())
            {
                string guid = sr.Properties["name"]?[0]?.ToString() ?? "";
                string displayName = sr.Properties["displayName"]?[0]?.ToString() ?? "";
                string path = sr.Properties["gPCFileSysPath"]?[0]?.ToString() ?? "";

                if (string.IsNullOrEmpty(guid))
                    continue;

                gpos[guid] = new GpoInfo
                {
                    Guid = guid,
                    DisplayName = displayName,
                    FileSysPath = path
                };

                _logger.LogInformation("Found GPO: {Name} ({Guid})", displayName, guid);
            }

            return gpos;
        }

        private void CollectOuLinks(GpoTreeInfo result, Dictionary<string, GpoInfo> gpoMap)
        {
            string domainPath = $"LDAP://{_domainDn}";

            using var entry = new DirectoryEntry(domainPath);
            using var searcher = new DirectorySearcher(entry)
            {
                Filter = "(objectClass=organizationalUnit)"
            };

            searcher.PropertiesToLoad.Add("distinguishedName");
            searcher.PropertiesToLoad.Add("name");
            searcher.PropertiesToLoad.Add("gPLink");

            foreach (SearchResult ou in searcher.FindAll())
            {
                string ouName = ou.Properties["name"]?[0]?.ToString() ?? "";
                string dn = ou.Properties["distinguishedName"]?[0]?.ToString() ?? "";

                var linkInfo = new OuGpoLink
                {
                    OuName = ouName,
                    DistinguishedName = dn
                };

                if (ou.Properties["gPLink"]?.Count > 0)
                {
                    string gpLink = ou.Properties["gPLink"][0].ToString();
                    ParseGpLink(gpLink, linkInfo, gpoMap);
                }

                result.OuLinks.Add(linkInfo);

                _logger.LogInformation(
                    "OU {OU} has {Count} linked GPOs",
                    ouName,
                    linkInfo.LinkedGpos.Count);
            }
        }

        private void ParseGpLink(
            string gpLink,
            OuGpoLink ou,
            Dictionary<string, GpoInfo> gpoMap)
        {
            var links = gpLink.Split(new[] { "][" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var link in links)
            {
                var clean = link.Replace("[", "").Replace("]", "");
                var parts = clean.Split(';');

                if (parts.Length == 0)
                    continue;

                // CN={GUID}
                var guidPart = parts[0];
                int start = guidPart.IndexOf('{');
                int end = guidPart.IndexOf('}');

                if (start < 0 || end < 0)
                    continue;

                string guid = guidPart.Substring(start + 1, end - start - 1);

                if (gpoMap.TryGetValue(guid, out var gpo))
                {
                    ou.LinkedGpos.Add(gpo);
                }
            }
        }
    }
}