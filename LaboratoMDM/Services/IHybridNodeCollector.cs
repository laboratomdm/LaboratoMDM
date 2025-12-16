using LaboratoMDM.Core.Models;

namespace LaboratoMDM.Services
{
    interface IHybridNodeCollector
    {
        NodeSnapshot Collect();
    }
}
