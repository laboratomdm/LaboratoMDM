using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.PolicyEngine.Domain
{
    public sealed class Presentation
    {
        public int Id {  get; set; }
        public string? PresentationId {  get; set; }
        public string? AdmlFile { get; set; }
        public List<PresentationElementEntity>? Elements {  get; set; }
    }

    public sealed class PresentationElementEntity
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? RefId { get; set; }
        public string? ParentElementId { get; set; }
        public string? DefaultValue { get; set; }
        public int DisplayOrder { get; set; }
        public string? Text { get; set; }
    }
}
