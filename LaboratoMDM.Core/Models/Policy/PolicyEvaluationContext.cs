namespace LaboratoMDM.Core.Models.Policy
{
    public sealed class PolicyEvaluationContext
    {
        public Version OsVersion { get; init; } = Environment.OSVersion.Version;
        public string OsProduct { get; init; } = "Windows";

        // TODO взляд в будущее:
        // public IReadOnlySet<string> InstalledFeatures
        // public HardwareInfo Hardware
        // public IReadOnlySet<string> Capabilities
    }

}
