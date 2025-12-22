using LaboratoMDM.PolicyEngine.Persistence;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Tests.Persistance;
public class AdmxRepositorySnapshotTests : IAsyncLifetime
{
    private SqliteConnection _conn = default!;
    private AdmxRepository _repo = default!;
    private readonly string StaticHash = Guid.NewGuid().ToString();

    public async Task InitializeAsync()
    {
        // создаём in-memory базу
        _conn = new SqliteConnection("Data Source=:memory:");
        await _conn.OpenAsync();

        // загружаем схему
        string schema = await System.IO.File.ReadAllTextAsync(
            @"C:\Users\Ivan\source\repos\LaboratoMDM\1__policies_tables.sql");

        await using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = schema;
            await cmd.ExecuteNonQueryAsync();
        }

        // создаём репозиторий с мапперами
        _repo = new AdmxRepository(
            _conn,
            new AdmxFileEntityMapper(),
            new PolicyNamespaceEntityMapper(),
            new PolicyCategoryEntityMapper(),
            new PolicyEntityMapper()
        );

        // Подготовка тестовых данных
        await PrepareTestData();
    }

    private async Task PrepareTestData()
    {
        // 1. Вставляем ADMX файл
        var admx = await _repo.CreateIfNotExists("test.admx", StaticHash);

        // 2. Вставляем namespace
        await using (var cmdNs = _conn.CreateCommand())
        {
            cmdNs.CommandText = @"
                    INSERT INTO PolicyNamespaces (Prefix, Namespace, AdmxFileId)
                    VALUES (@prefix, @ns, @admx)";
            cmdNs.Parameters.AddWithValue("@prefix", "test");
            cmdNs.Parameters.AddWithValue("@ns", "Microsoft.Policies.Test");
            cmdNs.Parameters.AddWithValue("@admx", admx.Id);
            await cmdNs.ExecuteNonQueryAsync();
        }

        // 3. Вставляем категорию
        int categoryId;
        await using (var cmdCat = _conn.CreateCommand())
        {
            cmdCat.CommandText = @"
                    INSERT INTO PolicyCategories
                    (Name, DisplayName, ExplainText, AdmxFileId)
                    VALUES (@name, @display, @explain, @admx);
                    SELECT last_insert_rowid();";
            cmdCat.Parameters.AddWithValue("@name", "TestCategory");
            cmdCat.Parameters.AddWithValue("@display", "Test Category");
            cmdCat.Parameters.AddWithValue("@explain", "Help text");
            cmdCat.Parameters.AddWithValue("@admx", admx.Id);
            categoryId = Convert.ToInt32(await cmdCat.ExecuteScalarAsync());
        }

        // 4. Вставляем политику
        int policyId;
        await using (var cmdPol = _conn.CreateCommand())
        {
            cmdPol.CommandText = @"
                    INSERT INTO Policies
                    (Name, Scope, RegistryKey, ValueName, EnabledValue, DisabledValue, SupportedOnRef, Hash, ParentCategoryRef)
                    VALUES (@name, @scope, @rk, @vn, @en, @dis, @sup, @hash, @cat);
                    SELECT last_insert_rowid();";
            cmdPol.Parameters.AddWithValue("@name", "TestPolicy");
            cmdPol.Parameters.AddWithValue("@scope", "User");
            cmdPol.Parameters.AddWithValue("@rk", @"SOFTWARE\Test");
            cmdPol.Parameters.AddWithValue("@vn", "Value");
            cmdPol.Parameters.AddWithValue("@en", 1);
            cmdPol.Parameters.AddWithValue("@dis", 0);
            cmdPol.Parameters.AddWithValue("@sup", "windows:SUPPORTED_Windows10");
            cmdPol.Parameters.AddWithValue("@hash", Guid.NewGuid().ToString());
            cmdPol.Parameters.AddWithValue("@cat", "TestCategory");
            policyId = Convert.ToInt32(await cmdPol.ExecuteScalarAsync());
        }

        // 5. Связываем политику с ADMX
        await using (var cmdMap = _conn.CreateCommand())
        {
            cmdMap.CommandText = @"
                    INSERT INTO PolicyAdmxMapping (PolicyId, AdmxFileId)
                    VALUES (@policy, @admx)";
            cmdMap.Parameters.AddWithValue("@policy", policyId);
            cmdMap.Parameters.AddWithValue("@admx", admx.Id);
            await cmdMap.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    public async Task LoadSnapshot_ShouldReturnFullSnapshot()
    {
        // Получаем ID ADMX файла
        var admx = await _repo.GetByHash(StaticHash);
        Assert.NotNull(admx);

        var snapshot = await _repo.LoadSnapshot(admx!.Id);

        Assert.NotNull(snapshot);
        Assert.Equal(admx.Id, snapshot.File.Id);
        Assert.NotEmpty(snapshot.Namespaces);
        Assert.NotEmpty(snapshot.Categories);
        Assert.NotEmpty(snapshot.Policies);

        // Проверяем, что политика связана с ADMX
        var policy = snapshot.Policies[0];
        Assert.Equal("TestPolicy", policy.Name);
    }

    public Task DisposeAsync()
    {
        return _conn.DisposeAsync().AsTask();
    }
}
