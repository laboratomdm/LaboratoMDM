using System.Text.Json.Serialization;

namespace LaboratoMDM.PolicyEngine.Domain
{
    public sealed class PolicyCategoryView
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("CategoryName")]
        public string CategoryName { get; set; }

        [JsonPropertyName("DisplayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("Children")]
        public IList<PolicyCategoryView> Childs { get; set; }
            = new List<PolicyCategoryView>();
    }
}
