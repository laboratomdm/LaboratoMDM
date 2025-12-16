using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models
{
    public class NodeSystemInfo
    {
        public Guid NodeId { get; set; } = Guid.NewGuid();
        public string HostName { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;

        // Hardware
        public string CPU { get; set; } = string.Empty;
        public int RAMGb { get; set; }
        public List<string> Disks { get; set; } = new();
        public List<string> GPU { get; set; } = new();

        // Network
        public List<string> IPAddresses { get; set; } = new();
        public List<string> MACAddresses { get; set; } = new();
        public string Motherboard { get; internal set; }
    }

}
