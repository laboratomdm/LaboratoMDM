using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence
{
    public sealed class PolicyRepository(
        SqliteConnection conn,
        IEntityMapper<PolicyEntity> mapper) : IPolicyRepository
    {
        private readonly SqliteConnection _conn = conn;
        private readonly IEntityMapper<PolicyEntity> _mapper = mapper;

        public async Task<PolicyEntity?> GetById(int policyId)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM Policies WHERE Id = @id
            """);

            cmd.Parameters.AddWithValue("@id", policyId);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync()
                ? _mapper.Map(reader)
                : null;
        }

        public async Task<PolicyEntity?> GetByHash(string hash)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM Policies WHERE Hash = @hash
            """);

            cmd.Parameters.AddWithValue("@hash", hash);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync()
                ? _mapper.Map(reader)
                : null;
        }

        public async Task<PolicyEntity> CreateIfNotExists(
            PolicyEntity policy)
        {
            var existing = await GetByHash(policy.Hash);
            if (existing != null)
                return existing;

            using var cmd = _conn.CreateCommand("""
                INSERT INTO Policies
                (Name, Scope, RegistryKey, ValueName,
                 EnabledValue, DisabledValue, SupportedOnRef, Hash)
                VALUES
                (@name, @scope, @rk, @vn, @en, @dis, @sup, @hash);
                SELECT last_insert_rowid();
            """);

            cmd.Parameters.AddWithValue("@name", policy.Name);
            cmd.Parameters.AddWithValue("@scope", policy.ScopeString);
            cmd.Parameters.AddWithValue("@rk", (object?)policy.RegistryKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@vn", (object?)policy.ValueName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@en", (object?)policy.EnabledValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dis", (object?)policy.DisabledValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sup", (object?)policy.SupportedOnRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hash", policy.Hash);

            var id = (long)(await cmd.ExecuteScalarAsync())!;
            
            policy.Id = (int)id;

            return policy;
        }

        public async Task LinkPolicyToAdmx(
            int policyId,
            int admxFileId)
        {
            using var cmd = _conn.CreateCommand("""
                INSERT OR IGNORE INTO PolicyAdmxMapping
                (PolicyId, AdmxFileId)
                VALUES (@p, @a)
            """);

            cmd.Parameters.AddWithValue("@p", policyId);
            cmd.Parameters.AddWithValue("@a", admxFileId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlyList<PolicyEntity>> FindApplicablePolicies(
            PolicyEvaluationContext context)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT *
                FROM Policies
                WHERE Scope IN ('None', 'Both', @scope)
            """);

            // пока упрощённо — machine/user будет решаться выше
            cmd.Parameters.AddWithValue("@scope", "Machine");

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();

            while (await reader.ReadAsync())
                list.Add(_mapper.Map(reader));

            return list;
        }

        public async Task<IReadOnlyList<PolicyEntity>> GetByCategory(
            int categoryId)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT p.*
                FROM Policies p
                JOIN PolicyCategoryMapping pcm
                    ON pcm.PolicyId = p.Id
                WHERE pcm.CategoryId = @cid
            """);

            cmd.Parameters.AddWithValue("@cid", categoryId);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();

            while (await reader.ReadAsync())
                list.Add(_mapper.Map(reader));

            return list;
        }
    }
}