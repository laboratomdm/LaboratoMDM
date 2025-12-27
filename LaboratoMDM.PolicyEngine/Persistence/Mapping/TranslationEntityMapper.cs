using LaboratoMDM.Core.Models.Policy;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping;

public sealed class TranslationEntityMapper : IEntityMapper<Translation>
{
    public Translation Map(SqliteDataReader r)
    {
        return new Translation
        {
            StringId = r.GetString(r.GetOrdinal("string_id")),
            LangCode = r.GetString(r.GetOrdinal("lang_code")),
            TextValue = r.GetString(r.GetOrdinal("text_value")),
            AdmlFilename = r.IsDBNull(r.GetOrdinal("adml_filename"))
                ? null
                : r.GetString(r.GetOrdinal("adml_filename")),
            CreatedAt = r.IsDBNull(r.GetOrdinal("created_at"))
                ? null
                : r.GetDateTime(r.GetOrdinal("created_at"))
        };
    }
}
