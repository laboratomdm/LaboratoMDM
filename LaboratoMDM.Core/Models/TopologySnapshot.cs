using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models
{
    public class TopologySnapshot
    {
        NodeIdentity Self;
        List<NodeIdentity> Children;
        List<NodeIdentity> Parents;
        DateTime Timestamp;
    }
}
