using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Mesh.Agent.Options
{
    public sealed class MeshOptions
    {
        public const string SectionName = "Mesh";

        public string MasterUrl { get; set; } = null!;
    }
}
