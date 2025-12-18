using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Tests.Support
{
    public sealed class InMemorySupportedOnCatalog : ISupportedOnCatalog
    {
        private readonly Dictionary<string, SupportedOnDefinition> _defs =
            new(StringComparer.OrdinalIgnoreCase);

        public void Add(SupportedOnDefinition def)
        {
            _defs[def.Name] = def;
        }

        public SupportedOnDefinition? Find(string name)
        {
            return _defs.TryGetValue(name, out var d) ? d : null;
        }
    }
}