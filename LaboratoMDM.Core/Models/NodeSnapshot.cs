namespace LaboratoMDM.Core.Models
{
    public class NodeSnapshot
    {
        public NodeSystemInfo SystemInfo { get; set; } = null!;
        public DomainInfo? AdInfo { get; set; } // Nullable для нод без AD
    }
}
