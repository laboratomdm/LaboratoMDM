using Microsoft.Data.Sqlite;
using System.Data;

namespace LaboratoMDM.PolicyEngine.Persistence.Schema;

public sealed class SqliteSchemaMigrator : ISchemaMigrator
{
    public async Task MigrateAsync(
        SqliteConnection connection,
        string migrationsPath,
        CancellationToken ct = default)
    {
        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException("Connection must be open");

        EnsureSchemaTable(connection);

        var migrations = Directory
            .GetFiles(migrationsPath, "*.sql")
            .Select(f => new MigrationInfo(f))
            .OrderBy(m => m.Version)
            .ToList();

        var applied = await GetAppliedMigrations(connection, ct);

        ValidateMigrations(migrations, applied);

        foreach (var migration in migrations)
        {
            if (applied.ContainsKey(migration.Version))
                continue;

            await ApplyMigration(connection, migration, ct);
        }
    }

    private static void EnsureSchemaTable(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand("""
            CREATE TABLE IF NOT EXISTS __SchemaVersion (
                Version INTEGER NOT NULL PRIMARY KEY,
                Hash TEXT NOT NULL,
                AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Name TEXT
            );
        """);

        cmd.ExecuteNonQuery();
    }

    private static async Task ApplyMigration(
        SqliteConnection conn,
        MigrationInfo migration,
        CancellationToken ct)
    {
        var sql = await File.ReadAllTextAsync(migration.FilePath, ct);

        await using var tx = (SqliteTransaction)await conn.BeginTransactionAsync(ct);

        try
        {
            // применяем SQL
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // фиксируем версию + hash
            await using (var versionCmd = conn.CreateCommand())
            {
                versionCmd.Transaction = tx;
                versionCmd.CommandText = """
                    INSERT INTO __SchemaVersion
                    (Version, Hash, Name)
                    VALUES (@v, @h, @n)
                """;

                versionCmd.Parameters.AddWithValue("@v", migration.Version);
                versionCmd.Parameters.AddWithValue("@h", migration.Hash);
                versionCmd.Parameters.AddWithValue("@n", migration.Name);

                await versionCmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        }
        catch(Exception ex)
        {
            await tx.RollbackAsync(ct);
            throw new InvalidOperationException(
                $"Failed to apply migration {migration.Version} ({migration.Name}). Reason: {ex.Message}");
        }
    }

    private static async Task<Dictionary<int, string>> GetAppliedMigrations(
        SqliteConnection conn,
        CancellationToken ct)
    {
        using var cmd = conn.CreateCommand("""
            SELECT Version, Hash
            FROM __SchemaVersion
        """);

        using var reader = await cmd.ExecuteReaderAsync(ct);
        var result = new Dictionary<int, string>();

        while (await reader.ReadAsync(ct))
        {
            result.Add(
                reader.GetInt32(0),
                reader.GetString(1));
        }

        return result;
    }

    private static void ValidateMigrations(
        IReadOnlyList<MigrationInfo> files,
        Dictionary<int, string> applied)
    {
        foreach (var appliedMigration in applied)
        {
            var file = files.FirstOrDefault(
                f => f.Version == appliedMigration.Key) ?? 
                throw new InvalidOperationException(
                    $"Applied migration {appliedMigration.Key} not found in migrations folder"
                    );

            if (!string.Equals(
                    file.Hash,
                    appliedMigration.Value,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Migration {file.Version} ({file.Name}) was modified after being applied");
            }
        }
    }
}