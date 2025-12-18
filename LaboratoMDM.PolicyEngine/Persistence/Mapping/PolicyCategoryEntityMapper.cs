using LaboratoMDM.PolicyEngine.Domain;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping
{
    public sealed class PolicyCategoryEntityMapper
        : IEntityMapper<PolicyCategoryEntity>
    {
        public PolicyCategoryEntity Map(SqliteDataReader r)
        {
            return new PolicyCategoryEntity
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                Name = r.GetString(r.GetOrdinal("Name")),
                ParentCategoryId = r.IsDBNull(r.GetOrdinal("ParentCategoryId"))
                    ? null
                    : r.GetInt32(r.GetOrdinal("ParentCategoryId"))
            };
        }
    }
}
