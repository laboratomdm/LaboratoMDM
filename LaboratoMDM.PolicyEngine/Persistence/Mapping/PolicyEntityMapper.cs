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
                DisplayName = r.IsDBNull(r.GetOrdinal("DisplayName"))
                    ? null
                    : r.GetString(r.GetOrdinal("DisplayName")),
                ExplainText = r.IsDBNull(r.GetOrdinal("ExplainText"))
                    ? null
                    : r.GetString(r.GetOrdinal("ExplainText")),
                ScopeString = r.GetString(r.GetOrdinal("Scope")),
                RegistryKey = r.GetString(r.GetOrdinal("RegistryKey")),
                ValueName = r.GetString(r.GetOrdinal("ValueName")),
                EnabledValue = r.IsDBNull(r.GetOrdinal("EnabledValue"))
                    ? null
                    : r.GetString(r.GetOrdinal("EnabledValue")),
                DisabledValue = r.IsDBNull(r.GetOrdinal("DisabledValue"))
                    ? null
                    : r.GetString(r.GetOrdinal("DisabledValue")),
                SupportedOnRef = r.IsDBNull(r.GetOrdinal("SupportedOnRef"))
                    ? null
                    : r.GetString(r.GetOrdinal("SupportedOnRef")),
                ParentCategory = r.IsDBNull(r.GetOrdinal("ParentCategoryRef"))
                    ? null
                    : r.GetString(r.GetOrdinal("ParentCategoryRef")),
                PresentationRef = r.IsDBNull(r.GetOrdinal("PresentationRef"))
                    ? null
                    : r.GetString(r.GetOrdinal("PresentationRef")),
                ClientExtension = r.IsDBNull(r.GetOrdinal("ClientExtension"))
                    ? null
                    : r.GetString(r.GetOrdinal("ClientExtension")),
                Hash = r.GetString(r.GetOrdinal("Hash"))
            };
        }
    }
}
