using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Tests.Support;

public class SqliteFileSchemaFixture : IAsyncLifetime
{
    public SqliteConnection Connection { get; private set; } = default!;
    private readonly string _schemaFilePath;

    public SqliteFileSchemaFixture()
    {
        // путь жестко задаем
        _schemaFilePath = @"C:\Users\Ivan\source\repos\LaboratoMDM\4__presentation_tables.sql";
        if (!File.Exists(_schemaFilePath))
            throw new FileNotFoundException("Schema file not found", _schemaFilePath);
    }

    public async Task InitializeAsync()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
        await Connection.OpenAsync();

        string sql = await File.ReadAllTextAsync(_schemaFilePath);
        await using var cmd = Connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("Inited");
    }

    public Task DisposeAsync()
    {
        Connection.Dispose();
        return Task.CompletedTask;
    }
}

