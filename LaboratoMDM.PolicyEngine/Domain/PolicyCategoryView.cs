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

        [JsonPropertyName("ExplainText")]
        public string? ExplainText { get; set; }

        [JsonPropertyName("Children")]
        public IReadOnlyList<PolicyCategoryView> Childs { get; set; }
            = Array.Empty<PolicyCategoryView>();
    }
}
