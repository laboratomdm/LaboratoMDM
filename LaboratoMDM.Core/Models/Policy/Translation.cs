namespace LaboratoMDM.Core.Models.Policy
{
    public class Translation
    {
        public string StringId { get; set; } = string.Empty;
        public string LangCode { get; set; } = string.Empty;
        public string TextValue { get; set; } = string.Empty;
        public string? AdmlFilename { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public sealed class TranslationComparer : IEqualityComparer<Translation>
    {
        public static readonly TranslationComparer Instance = new();

        public bool Equals(Translation? x, Translation? y) =>
            x != null && y != null &&
            string.Equals(x.StringId, y.StringId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.LangCode, y.LangCode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.AdmlFilename, y.AdmlFilename, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(Translation obj) =>
            HashCode.Combine(
                obj.StringId.ToLowerInvariant(),
                obj.LangCode.ToLowerInvariant(),
                obj.AdmlFilename?.ToLowerInvariant() ?? "");
    }
}
