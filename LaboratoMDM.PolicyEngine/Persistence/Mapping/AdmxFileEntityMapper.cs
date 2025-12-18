using LaboratoMDM.PolicyEngine.Domain;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping
{
    public sealed class AdmxFileEntityMapper : IEntityMapper<AdmxFileEntity>
    {
        public AdmxFileEntity Map(SqliteDataReader r)
        {
            return new AdmxFileEntity
            {
                Id = r.GetInt32(r.GetOrdinal("Id")),
                FileName = r.GetString(r.GetOrdinal("FileName")),
                FileHash = r.GetString(r.GetOrdinal("FileHash")),
                LoadedAt = r.GetDateTime(r.GetOrdinal("LoadedAt"))
            };
        }
    }
}
