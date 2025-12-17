using LaboratoMDM.Core.Models.Node;

namespace LaboratoMDM.Mesh.Master.Models
{
    public class AgentInfo
    {
        public string AgentId { get; set; } = Guid.NewGuid().ToString();
        public string HostName { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public DateTime LastHeartbeat { get; set; }
        public NodeFullInfo? LastNodeInfo { get; set; }
    }

}
