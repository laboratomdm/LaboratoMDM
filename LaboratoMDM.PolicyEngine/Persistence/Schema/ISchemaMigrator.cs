using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence.Schema
{
    public interface ISchemaMigrator
    {
        Task MigrateAsync(
            SqliteConnection connection,
            string migrationsPath,
            CancellationToken ct = default);
    }
}