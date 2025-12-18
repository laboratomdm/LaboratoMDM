using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence.Mapping
{
    public interface IEntityMapper<T>
    {
        T Map(SqliteDataReader reader);
    }
}
