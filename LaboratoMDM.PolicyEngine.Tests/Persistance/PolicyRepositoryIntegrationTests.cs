using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using LaboratoMDM.PolicyEngine.Tests.Support;

namespace LaboratoMDM.PolicyEngine.Tests.Persistance;

public class PolicyRepositoryIntegrationTests(SqliteFileSchemaFixture fixture)
        : IClassFixture<SqliteFileSchemaFixture>
{
    private readonly PolicyRepository _repo = new PolicyRepository(fixture.Connection, new PolicyEntityMapper());

    [Fact]
    public async Task CreateAndGetPolicy_FromFileSchema()
    {
        var policy = new PolicyEntity
        {
            Name = "TestPolicyFromFile",
            Scope = Core.Models.Policy.PolicyScope.Machine,
            RegistryKey = @"HKLM\Software\Test",
            ValueName = "Enabled",
            EnabledValue = "1",
            DisabledValue = "0",
            Hash = "hash-file-schema"
        };

        // Создание политики
        var created = await _repo.CreateIfNotExists(policy);

        // Получение по Id
        var loaded = await _repo.GetById(created.Id);

        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded!.Id);
        Assert.Equal("TestPolicyFromFile", loaded.Name);
    }

    [Fact]
    public async Task GetByHash_Should_ReturnPolicy_WhenExists()
    {
        var policy = new PolicyEntity
        {
            Name = "HashTest",
            Scope = PolicyScope.Machine,
            Hash = Guid.NewGuid().ToString()
        };

        var created = await _repo.CreateIfNotExists(policy);
        var loaded = await _repo.GetByHash(created.Hash);

        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded!.Id);
    }

    [Fact]
    public async Task FindApplicablePolicies_Should_ReturnList()
    {
        var context = new PolicyEvaluationContext(); // упрощённо

        var result = await _repo.FindApplicablePolicies(context);

        Assert.NotNull(result);
    }
}

