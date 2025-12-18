using LaboratoMDM.PolicyEngine.Domain;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping
{
    public sealed class PolicyEntityMapper : IEntityMapper<PolicyEntity>
    {
        public PolicyEntity Map(SqliteDataReader r)
        {
            return new PolicyEntity
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                Name = r.GetString(r.GetOrdinal("Name")),
                ScopeString = r.GetString(r.GetOrdinal("Scope")),
                RegistryKey = r.GetString(r.GetOrdinal("RegistryKey")),
                ValueName = r.GetString(r.GetOrdinal("ValueName")),
                EnabledValue = r.IsDBNull(r.GetOrdinal("EnabledValue"))
                    ? null
                    : r.GetInt32(r.GetOrdinal("EnabledValue")),
                DisabledValue = r.IsDBNull(r.GetOrdinal("DisabledValue"))
                    ? null
                    : r.GetInt32(r.GetOrdinal("DisabledValue")),
                SupportedOnRef = r.IsDBNull(r.GetOrdinal("SupportedOnRef"))
                    ? null
                    : r.GetString(r.GetOrdinal("SupportedOnRef")),
                Hash = r.GetString(r.GetOrdinal("Hash"))
            };
        }
    }
}
