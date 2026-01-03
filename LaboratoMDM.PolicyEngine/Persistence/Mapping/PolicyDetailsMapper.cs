using LaboratoMDM.Core.Models.Policy;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping
{
    public sealed class PolicyDetailsViewMapper
        : IEntityMapper<PolicyDetailsView>
    {
        public PolicyDetailsView Map(SqliteDataReader r)
        {
            var json = r.GetString(r.GetOrdinal("PolicyDetails"));

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<PolicyDetailsView>(json, options)
                   ?? throw new JsonException("Failed to deserialize PolicyDetailsView");
        }
    }
}

