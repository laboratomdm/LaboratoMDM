using LaboratoMDM.PolicyEngine.Domain;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping
{
    public sealed class PolicyDetailsViewMapper
        : IEntityMapper<PolicyEntity>
    {
        public PolicyEntity Map(SqliteDataReader r)
        {
            var json = r.GetString(r.GetOrdinal("PolicyDetails"));

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<PolicyEntity>(json, options)
                   ?? throw new JsonException("Failed to deserialize PolicyDetailsView");
        }
    }
}

