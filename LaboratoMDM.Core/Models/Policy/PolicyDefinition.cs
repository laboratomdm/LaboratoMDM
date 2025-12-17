namespace LaboratoMDM.Core.Models.Policy
{
    public enum PolicyScope
    {
        User,
        Machine
    }
    public sealed class PolicyDefinition
    {
        public string Name { get; init; } = string.Empty;
        public PolicyScope Scope { get; init; }
        public string RegistryKey { get; init; } = string.Empty;
        public string ValueName { get; init; } = string.Empty;

        public int? EnabledValue { get; init; }
        public int? DisabledValue { get; init; }

        public IReadOnlyList<string> ListKeys { get; init; } = Array.Empty<string>();

        public string? SupportedOnRef { get; init; }
    }
}
