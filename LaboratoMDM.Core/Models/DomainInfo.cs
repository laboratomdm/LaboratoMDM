using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models
{
    public class DomainInfo
    {
        public string DomainName { get; set; } = string.Empty;
        public string ForestName { get; set; } = string.Empty;
        public bool IsDomainController { get; set; }
        public bool IsPdcEmulator { get; set; }

        public List<string> DomainControllers { get; set; } = new();
        public List<string> Trusts { get; set; } = new();
        public List<string> Sites { get; set; } = new();

        // FSMO роли
        public string? RidMaster { get; set; }
        public string? InfrastructureMaster { get; set; }
        public string? SchemaMaster { get; set; }
        public string? DomainNamingMaster { get; set; }

        // Свойства AD топологии
        public Dictionary<string, List<string>> DcReplicationPartners { get; set; } = new();
    }
}
