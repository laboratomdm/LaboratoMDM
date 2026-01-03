using System.Text.Json;
using System.Text.Json.Serialization;

namespace LaboratoMDM.Core.Models.Policy
{
    public sealed class PolicyView
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public required string Hash { get; set; }
        public required string Scope { get; set; }
        public string? ParentCategoryRef { get; set; }
        public string? SupportedOnRef { get; set; }
        public string? ClientExtension { get; set; }
    }

    public class PresentationView
    {
        public int Id { get; set; }
        public required string PresentationId { get; set; }
        public required string AdmlFile { get; set; }
        public List<PresentationElementView> Elements { get; set; }
    }

    public class PresentationElementView
    {
        public int Id { get; set; }
        public required string Type { get; set; }
        public required string RefId { get; set; }
        public int? ParentElementId { get; set; }
        public string? DefaultValue { get; set; }
        public string? Text { get; set; }
    }

    public class PolicyElementView
    {
        public int Id { get; set; }
        public required string ElementId { get; set; }
        public required string Type { get; set; }
        public string? ValueName { get; set; }

        [JsonConverter(typeof(IntBooleanJsonConverter))]
        public bool? Required { get; set; }
        public int? MaxLength { get; set; }

        // For Text/Multitext
        public string? MaxStrings { get; set; }

        [JsonConverter(typeof(IntBooleanJsonConverter))]
        public bool? Expandable { get; set; }

        // For Decimal
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }

        //For List
        public string? ValuePrefix { get; set; }

        [JsonConverter(typeof(IntBooleanJsonConverter))]
        public bool? ExplicitValue { get; set; }

        [JsonConverter(typeof(IntBooleanJsonConverter))]
        public bool? Additive {  get; set; }

        public List<PolicyElementItemView> Items { get; set; }
    }

    public class PolicyElementItemView
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ParentType { get; set; }
        public string Type { get; set; }
        public string ValueType { get; set; }
        public string? ValueName { get; set; }

        [JsonConverter(typeof(IntBooleanJsonConverter))]
        public bool? Required { get; set; }
        public int? ParentId { get; set; }
        public string? DisplayName { get; set; }
    }



    public sealed class PolicyDetailsView
    {
        [JsonPropertyName("Policy")]
        public required PolicyView Policy { get; set; }

        [JsonPropertyName("Presentation")]
        public PresentationView? Presentation { get; set; }

        [JsonPropertyName("PolicyElements")]
        public List<PolicyElementView> PolicyElements { get; set; } = new();
    }

    public sealed class IntBooleanJsonConverter : JsonConverter<bool?>
    {
        public override bool? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number => reader.GetInt32() != 0,
                JsonTokenType.String => bool.TryParse(reader.GetString(), out var b) ? b : null,
                JsonTokenType.Null => null,
                _ => throw new JsonException($"Cannot convert {reader.TokenType} to bool?")
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            bool? value,
            JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteBooleanValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
