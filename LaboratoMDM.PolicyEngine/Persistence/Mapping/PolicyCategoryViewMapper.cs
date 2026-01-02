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
    public sealed class PolicyCategoryViewMapper
    : IEntityMapper<IReadOnlyList<PolicyCategoryView>>
    {
        public IReadOnlyList<PolicyCategoryView> Map(SqliteDataReader r)
        {
            var json = r.GetString(r.GetOrdinal("CategoryTreeJson"));

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<IReadOnlyList<PolicyCategoryView>>(json, options);
        }
    }
}

