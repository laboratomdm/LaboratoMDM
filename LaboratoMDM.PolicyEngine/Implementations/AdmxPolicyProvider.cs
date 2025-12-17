#nullable enable
using LaboratoMDM.Core.Models.Policy;
using System.Xml.Linq;

namespace LaboratoMDM.PolicyEngine.Implementations
{
    public sealed class AdmxPolicyProvider : IPolicyProvider
    {
        private static readonly XNamespace Ns =
            "http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions";

        private readonly string _policyDefinitionsPath;
        private readonly Dictionary<string, PolicyDefinition> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        public AdmxPolicyProvider(string policyDefinitionsPath)
        {
            if (!Directory.Exists(policyDefinitionsPath))
                throw new DirectoryNotFoundException(policyDefinitionsPath);

            _policyDefinitionsPath = policyDefinitionsPath;
            LoadInternal();
        }

        public IReadOnlyList<PolicyDefinition> LoadPolicies() =>
            _cache.Values.ToList();

        public PolicyDefinition? FindPolicy(string name) =>
            _cache.TryGetValue(name, out var p) ? p : null;

        private void LoadInternal()
        {
            foreach (var file in Directory.EnumerateFiles(_policyDefinitionsPath, "*.admx"))
            {
                TryLoadFile(file);
            }
        }

        private void TryLoadFile(string file)
        {
            try
            {
                var doc = XDocument.Load(file);

                foreach (var p in doc.Descendants(Ns + "policy"))
                {
                    var name = (string?)p.Attribute("name");
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var scope = ((string?)p.Attribute("class")) == "Machine"
                        ? PolicyScope.Machine
                        : PolicyScope.User;

                    var policy = new PolicyDefinition
                    {
                        Name = name,
                        Scope = scope,
                        RegistryKey = (string?)p.Attribute("key") ?? string.Empty,
                        ValueName = (string?)p.Attribute("valueName") ?? string.Empty,
                        EnabledValue = ReadDecimal(p, "enabledValue"),
                        DisabledValue = ReadDecimal(p, "disabledValue"),
                        SupportedOnRef = p.Element(Ns + "supportedOn")?.Attribute("ref")?.Value,
                        ListKeys = ReadListKeys(p)
                    };

                    _cache.TryAdd(policy.Name, policy);
                }
            }
            catch
            {
                // битые или несовместимые ADMX намеренно игнорируются
            }
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
    }
}