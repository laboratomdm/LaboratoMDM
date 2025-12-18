using LaboratoMDM.PolicyEngine.Domain;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping
{
    public sealed class PolicyNamespaceEntityMapper
        : IEntityMapper<PolicyNamespaceEntity>
    {
        public PolicyNamespaceEntity Map(SqliteDataReader r)
        {
            return new PolicyNamespaceEntity
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                AdmxFileId = r.GetInt32(r.GetOrdinal("AdmxFileId")),
                Prefix = r.GetString(r.GetOrdinal("Prefix")),
                Namespace = r.GetString(r.GetOrdinal("Namespace"))
            };
        }
    }
}
