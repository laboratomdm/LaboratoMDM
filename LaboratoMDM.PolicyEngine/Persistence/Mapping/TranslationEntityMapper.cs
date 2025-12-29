using LaboratoMDM.Core.Models.Policy;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping;

public sealed class TranslationEntityMapper : IEntityMapper<Translation>
{
    public Translation Map(SqliteDataReader r)
    {
        return new Translation
        {
            StringId = r.GetString(r.GetOrdinal("StringId")),
            LangCode = r.GetString(r.GetOrdinal("LangCode")),
            TextValue = r.GetString(r.GetOrdinal("TextValue")),
            AdmlFilename = r.IsDBNull(r.GetOrdinal("AdmlFilename"))
                ? null
                : r.GetString(r.GetOrdinal("AdmlFilename")),
            CreatedAt = r.IsDBNull(r.GetOrdinal("CreatedAt"))
                ? null
                : r.GetDateTime(r.GetOrdinal("CreatedAt"))
        };
    }
}
