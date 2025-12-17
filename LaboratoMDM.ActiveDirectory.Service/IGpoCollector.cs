using LaboratoMDM.Core.Models;

namespace LaboratoMDM.ActiveDirectory.Service
{
    public interface IGpoCollector
    {
        /// <summary>
        /// Собирает дерево всех GPO в домене
        /// </summary>
        GpoTopology? Collect();
    }
}
