using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models
{
    record NodeIdentity
    {
        Guid NodeId;
        readonly string Hostname;
        readonly string Fqdn;
        readonly string Ip;
        NodeRole Role;
        readonly string Domain;
    }

    enum NodeRole
    {
        Child, 
        Parent, 
        Master
    }
}
