#nullable enable
using LaboratoMDM.Core.Models.Policy;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    public sealed class AdmxPolicyProvider : IPolicyProvider
    {
        private static readonly XNamespace Ns =
            "http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions";

        private readonly Dictionary<string, PolicyDefinition> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ILogger<AdmxPolicyProvider> _logger;

        /// <summary>
        /// Путь к папке с ADMX файлами
        /// </summary>
        public string PolicyDefinitionsPath { get; }

        public AdmxPolicyProvider(string policyDefinitionsPath, ILogger<AdmxPolicyProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!Directory.Exists(policyDefinitionsPath))
            {
                _logger.LogError("ADMX folder not found: {Path}", policyDefinitionsPath);
                throw new DirectoryNotFoundException($"ADMX folder not found: {policyDefinitionsPath}");
            }

            PolicyDefinitionsPath = policyDefinitionsPath;
            LoadInternal();
        }

        public IReadOnlyList<PolicyDefinition> LoadPolicies() => _cache.Values.ToList();

        public PolicyDefinition? FindPolicy(string name) =>
            _cache.TryGetValue(name, out var p) ? p : null;

        private void LoadInternal()
        {
            _logger.LogInformation("Starting to load ADMX policies from {Path}", PolicyDefinitionsPath);

            int totalLoaded = 0;

            foreach (var file in Directory.EnumerateFiles(PolicyDefinitionsPath, "*.admx"))
            {
                totalLoaded += TryLoadFile(file);
            }

            _logger.LogInformation("Finished loading ADMX policies. Total loaded: {Count}", totalLoaded);
        }

        private int TryLoadFile(string file)
        {
            int loadedCount = 0;

            try
            {
                var doc = XDocument.Load(file);
                foreach (var p in doc.Descendants(Ns + "policy"))
                {
                    var name = (string?)p.Attribute("name");
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var classAttr = ((string?)p.Attribute("class"))?.Trim();
                    PolicyScope scope = classAttr switch
                    {
                        "Machine" => PolicyScope.Machine,
                        "User" => PolicyScope.User,
                        "Both" => PolicyScope.Both,
                        _ => PolicyScope.None
                    };

                    var policy = new PolicyDefinition
                    {
                        Name = name,
                        Scope = scope,
                        RegistryKey = (string?)p.Attribute("key") ?? string.Empty,
                        ValueName = (string?)p.Attribute("valueName") ?? string.Empty,
                        EnabledValue = ReadDecimal(p, "enabledValue"),
                        DisabledValue = ReadDecimal(p, "disabledValue"),
                        SupportedOnRef = p.Element(Ns + "supportedOn")?.Attribute("ref")?.Value,
                        ListKeys = ReadListKeys(p),
                        RequiredCapabilities = ReadCapabilities(p),
                        RequiredHardware = ReadHardwareRequirements(p)
                    };

                    if (_cache.TryAdd(policy.Name, policy))
                    {
                        loadedCount++;
                        _logger.LogDebug("Loaded policy: {Name} ({Scope})", policy.Name, policy.Scope);
                    }
                    else
                    {
                        _logger.LogWarning("Policy {Name} already exists in cache. Skipping.", policy.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load ADMX file '{File}'", file);
            }

            return loadedCount;
        }

        private static int? ReadDecimal(XElement policy, string element)
        {
            var val = policy.Element(Ns + element)
                            ?.Element(Ns + "decimal")
                            ?.Attribute("value")?.Value;

            return val != null && int.TryParse(val, out var i) ? i : null;
        }

        private static IReadOnlyList<string> ReadListKeys(XElement policy)
        {
            return policy.Descendants(Ns + "list")
                         .Select(l => (string?)l.Attribute("key"))
                         .Where(k => !string.IsNullOrWhiteSpace(k))
                         .Cast<string>()
                         .ToList();
        }

        private static IReadOnlyList<string> ReadCapabilities(XElement policy)
        {
            return policy.Descendants(Ns + "capability")
                         .Select(c => (string?)c.Attribute("name"))
                         .Where(n => !string.IsNullOrWhiteSpace(n))
                         .Cast<string>()
                         .ToList();
        }

        private static IReadOnlyList<string> ReadHardwareRequirements(XElement policy)
        {
            return policy.Descendants(Ns + "hardwareRequirement")
                         .Select(h => (string?)h.Attribute("name"))
                         .Where(n => !string.IsNullOrWhiteSpace(n))
                         .Cast<string>()
                         .ToList();
        }
    }
}