using LaboratoMDM.Core.Models;
using System.DirectoryServices.ActiveDirectory;

namespace LaboratoMDM.ActiveDirectory.Core.Models
{
    class ADTopologySnapshot : TopologySnapshot
    {
        List<Domain> Domains;
    }
}
