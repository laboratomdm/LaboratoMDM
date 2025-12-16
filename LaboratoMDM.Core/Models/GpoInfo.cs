namespace LaboratoMDM.Core.Models
{
    public class GpoInfo
    {
        public string Guid { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FileSysPath { get; set; } = string.Empty;
    }

    public class OuGpoLink
    {
        public string OuName { get; set; } = string.Empty;
        public string DistinguishedName { get; set; } = string.Empty;
        public List<GpoInfo> LinkedGpos { get; set; } = new();
    }

    public class GpoTreeInfo
    {
        public string Domain { get; set; } = string.Empty;
        public List<GpoInfo> AllGpos { get; set; } = new();
        public List<OuGpoLink> OuLinks { get; set; } = new();
    }
}