namespace LaboratoMDM.Core.Models
{
    public class GpoInfo
    {
        public string Guid { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FileSysPath { get; set; } = string.Empty;

        public bool UserEnabled { get; set; }
        public bool ComputerEnabled { get; set; }

        public string? WmiFilter { get; set; }
    }

    public class GpoLinkInfo
    {
        public GpoInfo Gpo { get; set; } = default!;
        public int LinkOrder { get; set; }
        public bool Enabled { get; set; }
        public bool Enforced { get; set; }
    }

    public class OuGpoLink
    {
        public string OuName { get; set; } = string.Empty;
        public string DistinguishedName { get; set; } = string.Empty;

        public bool BlockInheritance { get; set; }

        public List<GpoLinkInfo> GpoLinks { get; set; } = new();
    }

    public class GpoTopology
    {
        public string Domain { get; set; } = string.Empty;
        public List<GpoInfo> AllGpos { get; set; } = new();
        public List<OuGpoLink> OuTopology { get; set; } = new();
    }
}
