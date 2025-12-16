using LaboratoMDM.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Services
{
    public interface INodeSystemInfoCollector
    {
        NodeSystemInfo Collect();
    }
}
