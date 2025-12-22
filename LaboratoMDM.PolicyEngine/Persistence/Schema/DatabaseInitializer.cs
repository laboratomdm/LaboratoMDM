using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.PolicyEngine.Persistence.Schema;

public static class DatabaseInitializer
{
    /// <summary>
    /// Создает файл базы SQLite, если его нет, и применяет миграции из указанной папки.
    /// </summary>
    /// <param name="dbFilePath">Путь к файлу базы данных SQLite.</param>
    /// <param name="migrationsPath">Путь к папке с SQL миграциями.</param>
    /// <returns>Открытое соединение с базой.</returns>
    public static async Task<SqliteConnection> InitializeAsync(
        string dbFilePath,
        string migrationsPath,
        ILogger logger)
    {
        if (!File.Exists(dbFilePath))
        {
            logger.LogInformation("Database file {dbFilePath} not found. Creating new one...", dbFilePath);
            using var fs = File.Create(dbFilePath);
        }

        var connectionString = $"Data Source={dbFilePath}";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var migrator = new SqliteSchemaMigrator();
        await migrator.MigrateAsync(connection, migrationsPath);

        logger.LogInformation("Database initialized and migrations applied.");
        return connection;
    }
}

