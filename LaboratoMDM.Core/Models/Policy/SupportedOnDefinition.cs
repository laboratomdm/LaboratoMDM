using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models.Policy
{
    public sealed class SupportedOnDefinition
    {
        public string Name { get; init; } = null!;
        public string? DisplayNameRef { get; init; }

        public SupportedOnExpression RootExpression { get; init; } = null!;
    }

    public interface ISupportedOnCatalog
    {
        SupportedOnDefinition? Find(string name);
    }
}
