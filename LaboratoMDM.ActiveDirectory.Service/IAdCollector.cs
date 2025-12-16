using LaboratoMDM.Core.Models;

namespace LaboratoMDM.ActiveDirectory.Service
{
    public interface IAdCollector
    {
        DomainInfo? Collect();
    }
}
