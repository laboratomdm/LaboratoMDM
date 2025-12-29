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
            return await reader.ReadAsync() ? _mapper.Map(reader) : null;
        }

        public async Task<PolicyEntity?> GetByHash(string hash)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM Policies WHERE Hash = @hash
            """);

            cmd.Parameters.AddWithValue("@hash", hash);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? _mapper.Map(reader) : null;
        }

        public async Task<IReadOnlyList<PolicyEntity>> GetByHashes(IReadOnlyCollection<string> hashes)
        {
            if (hashes.Count == 0)
                return Array.Empty<PolicyEntity>();

            using var cmd = _conn.CreateCommand("""
                SELECT *
                FROM Policies
                WHERE Hash IN (
                    SELECT value FROM json_each(@hashes)
                )
            """);

            cmd.Parameters.AddWithValue("@hashes", System.Text.Json.JsonSerializer.Serialize(hashes));

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();
            while (await reader.ReadAsync())
                list.Add(_mapper.Map(reader));
            return list;
        }

        public async Task<PolicyEntity> CreateIfNotExists(PolicyEntity policy)
        {
            await using var tx = await _conn.BeginTransactionAsync();

            var existing = await GetByHash(policy.Hash);
            if (existing != null) return existing;

            using var cmd = _conn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
                INSERT INTO Policies
                (Name, DisplayName, ExplainText, Scope, RegistryKey, ValueName, EnabledValue, DisabledValue, SupportedOnRef, ParentCategoryRef, PresentationRef, ClientExtension, Hash)
                VALUES
                (@name, @dn, @et, @scope, @rk, @vn, @en, @dis, @sup, @pcr, @pres, @ce, @hash);
                SELECT last_insert_rowid();
            """;

            cmd.Parameters.AddWithValue("@name", policy.Name);
            cmd.Parameters.AddWithValue("@dn", (object?)policy.DisplayName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@et", (object?)policy.ExplainText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@scope", policy.ScopeString);
            cmd.Parameters.AddWithValue("@rk", (object?)policy.RegistryKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@vn", (object?)policy.ValueName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@en", (object?)policy.EnabledValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dis", (object?)policy.DisabledValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sup", (object?)policy.SupportedOnRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pcr", (object?)policy.ParentCategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pres", (object?)policy.PresentationRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ce", (object?)policy.ClientExtension ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hash", policy.Hash);

            policy.Id = (int)(long)(await cmd.ExecuteScalarAsync()!);

            await SavePolicyElementsAsync(policy.Id, policy.Elements, (SqliteTransaction)tx);

            await tx.CommitAsync();
            return policy;
        }

        public async Task CreatePoliciesBatch(IReadOnlyList<PolicyEntity> policies)
        {
            if (policies.Count == 0) return;

            await using var tx = await _conn.BeginTransactionAsync();

            using var cmd = _conn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO Policies
                (Name, DisplayName, ExplainText, Scope, RegistryKey, ValueName, EnabledValue, DisabledValue, SupportedOnRef, ParentCategoryRef, PresentationRef, ClientExtension, Hash)
                VALUES
                (@name, @dn, @et, @scope, @rk, @vn, @en, @dis, @sup, @pcr, @pres, @ce, @hash);
            """;

            var name = cmd.CreateParameter(); name.ParameterName = "@name";
            var dn = cmd.CreateParameter(); dn.ParameterName = "@pn";
            var et = cmd.CreateParameter(); et.ParameterName = "@et";
            var scope = cmd.CreateParameter(); scope.ParameterName = "@scope";
            var rk = cmd.CreateParameter(); rk.ParameterName = "@rk";
            var vn = cmd.CreateParameter(); vn.ParameterName = "@vn";
            var en = cmd.CreateParameter(); en.ParameterName = "@en";
            var dis = cmd.CreateParameter(); dis.ParameterName = "@dis";
            var sup = cmd.CreateParameter(); sup.ParameterName = "@sup";
            var pcr = cmd.CreateParameter(); pcr.ParameterName = "@pcr";
            var pres = cmd.CreateParameter(); pres.ParameterName = "@pres";
            var ce = cmd.CreateParameter(); pres.ParameterName = "@ce";
            var hash = cmd.CreateParameter(); hash.ParameterName = "@hash";

            cmd.Parameters.AddRange(new[] { name, scope, rk, vn, en, dis, sup, pcr, pres, ce, hash });

            foreach (var p in policies)
            {
                name.Value = p.Name;
                dn.Value = (object?)p.DisplayName ?? DBNull.Value;
                et.Value = (object?)p.ExplainText ?? DBNull.Value;
                scope.Value = p.ScopeString;
                rk.Value = (object?)p.RegistryKey ?? DBNull.Value;
                vn.Value = (object?)p.ValueName ?? DBNull.Value;
                en.Value = (object?)p.EnabledValue ?? DBNull.Value;
                dis.Value = (object?)p.DisabledValue ?? DBNull.Value;
                sup.Value = (object?)p.SupportedOnRef ?? DBNull.Value;
                pcr.Value = (object?)p.ParentCategory ?? DBNull.Value;
                pres.Value = (object?)p.PresentationRef ?? DBNull.Value;
                ce.Value = (object?)p.ClientExtension ?? DBNull.Value;
                hash.Value = p.Hash;

                await cmd.ExecuteNonQueryAsync();

                // Получаем Id вставленной или существующей политики
                using var getIdCmd = _conn.CreateCommand();
                getIdCmd.Transaction = (SqliteTransaction)tx;
                getIdCmd.CommandText = "SELECT Id FROM Policies WHERE Hash = @hash";
                getIdCmd.Parameters.AddWithValue("@hash", p.Hash);
                p.Id = (int)(long)(await getIdCmd.ExecuteScalarAsync()!);

                await SavePolicyElementsAsync(p.Id, p.Elements, (SqliteTransaction)tx);
            }

            await tx.CommitAsync();
        }

        private async Task SavePolicyElementsAsync(int policyId, IEnumerable<PolicyElementEntity> elements, SqliteTransaction tx)
        {
            await using var cmd = _conn.CreateCommand();
            cmd.Transaction = tx;

            cmd.CommandText = @"
INSERT INTO PolicyElements
(PolicyId, ElementId, Type, ValueName, Required, MaxLength, ClientExtension)
VALUES (@policyId, @elementId, @type, @valueName, @required, @maxLength, @clientExt)
ON CONFLICT(PolicyId, ElementId) DO NOTHING;
SELECT Id FROM PolicyElements WHERE PolicyId = @policyId AND ElementId = @elementId;
";

            var policyParam = cmd.CreateParameter(); policyParam.ParameterName = "@policyId";
            var elementParam = cmd.CreateParameter(); elementParam.ParameterName = "@elementId";
            var typeParam = cmd.CreateParameter(); typeParam.ParameterName = "@type";
            var valueParam = cmd.CreateParameter(); valueParam.ParameterName = "@valueName";
            var requiredParam = cmd.CreateParameter(); requiredParam.ParameterName = "@required";
            var maxParam = cmd.CreateParameter(); maxParam.ParameterName = "@maxLength";
            var clientParam = cmd.CreateParameter(); clientParam.ParameterName = "@clientExt";

            cmd.Parameters.AddRange(new[] { policyParam, elementParam, typeParam, valueParam, requiredParam, maxParam, clientParam });

            foreach (var e in elements)
            {
                policyParam.Value = policyId;
                elementParam.Value = e.IdName;
                typeParam.Value = e.Type;
                valueParam.Value = (object?)e.ValueName ?? DBNull.Value;
                requiredParam.Value = (e.Required ?? false) ? 1 : 0;
                maxParam.Value = (object?)e.MaxLength ?? DBNull.Value;
                clientParam.Value = (object?)e.ClientExtension ?? DBNull.Value;

                e.Id = (int)(long)(await cmd.ExecuteScalarAsync()!);
            }
        }

        public async Task LinkPolicyToAdmx(int policyId, int admxFileId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT OR IGNORE INTO PolicyAdmxMapping
                (PolicyId, AdmxFileId)
                VALUES (@p, @a)
            """;

            cmd.Parameters.AddWithValue("@p", policyId);
            cmd.Parameters.AddWithValue("@a", admxFileId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task LinkPoliciesToAdmxBatch(int admxFileId, IReadOnlyList<int> policyIds)
        {
            await using var tx = await _conn.BeginTransactionAsync();

            using var cmd = _conn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO PolicyAdmxMapping
                (PolicyId, AdmxFileId)
                VALUES (@p, @a)
            """;

            var p = cmd.CreateParameter(); p.ParameterName = "@p";
            var a = cmd.CreateParameter(); a.ParameterName = "@a";
            cmd.Parameters.Add(p);
            cmd.Parameters.Add(a);

            foreach (var id in policyIds)
            {
                p.Value = id;
                a.Value = admxFileId;
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<IReadOnlyList<PolicyEntity>> FindApplicablePolicies(PolicyEvaluationContext context)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT *
                FROM Policies
                WHERE Scope IN ('None', 'Both', @scope)
            """;

            cmd.Parameters.AddWithValue("@scope", "Machine");

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();
            while (await reader.ReadAsync())
                list.Add(_mapper.Map(reader));
            return list;
        }

        public async Task<IReadOnlyList<PolicyEntity>> GetByCategory(int categoryId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT p.*
                FROM Policies p
                JOIN PolicyCategoryMapping pcm
                    ON pcm.PolicyId = p.Id
                WHERE pcm.CategoryId = @cid
            """;

            cmd.Parameters.AddWithValue("@cid", categoryId);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();
            while (await reader.ReadAsync())
                list.Add(_mapper.Map(reader));
            return list;
        }
    }
}
