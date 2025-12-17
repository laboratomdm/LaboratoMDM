using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models
{
    public class RsopInput
    {
        public string ComputerDn { get; set; } = string.Empty;
        public GpoTopology Topology { get; set; } = default!;
    }

    public class RsopResult
    {
        public string ComputerDn { get; set; } = string.Empty;
        public List<RsopAppliedGpo> AppliedGpos { get; set; } = new();
    }

    public class RsopAppliedGpo
    {
        public string GpoName { get; set; } = string.Empty;
        public string GpoGuid { get; set; } = string.Empty;
        public int Precedence { get; set; }
        public bool Enforced { get; set; }
    }

}
