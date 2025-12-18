using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.Core.Models.Policy
{
    public abstract class SupportedOnExpression {}

    public sealed class SupportedOnOr : SupportedOnExpression
    {
        public IList<SupportedOnExpression> Items { get; init; } =
            Array.Empty<SupportedOnExpression>();
    }

    public sealed class SupportedOnAnd : SupportedOnExpression
    {
        public IList<SupportedOnExpression> Items { get; init; } =
            Array.Empty<SupportedOnExpression>();
    }

    public sealed class SupportedOnReference : SupportedOnExpression
    {
        public string Ref { get; init; } = null!;
    }

    public sealed class SupportedOnRange : SupportedOnExpression
    {
        public string Ref { get; init; } = null!;
        public int? MinVersionIndex { get; init; }
        public int? MaxVersionIndex { get; init; }
    }
}

