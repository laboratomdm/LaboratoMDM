#nullable enable

using LaboratoMDM.Mesh.Agent.Domain;
using LaboratoMDM.Mesh.Agent.Persistance.Abstractions;
using LaboratoMDM.Mesh.Agent.Persistance.Mapping;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.Mesh.Agent.Persistance
{
    public sealed class AgentPolicyRepository : IAgentPolicyRepository
    {
        private readonly SqliteConnection _conn;
        private readonly IEntityMapper<AgentPolicyEntity> _policyMapper;
        private readonly IEntityMapper<AgentPolicyElementEntity> _elementMapper;
        private readonly IEntityMapper<AgentPolicyComplianceEntity> _complianceMapper;

        public AgentPolicyRepository(
            SqliteConnection conn,
            IEntityMapper<AgentPolicyEntity> policyMapper,
            IEntityMapper<AgentPolicyElementEntity> elementMapper,
            IEntityMapper<AgentPolicyComplianceEntity> complianceMapper)
        {
            _conn = conn;
            _policyMapper = policyMapper;
            _elementMapper = elementMapper;
            _complianceMapper = complianceMapper;
        }

        public async Task<AgentPolicyEntity?> GetPolicyByHashAsync(string hash)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Policies WHERE Hash = @hash";
            cmd.Parameters.AddWithValue("@hash", hash);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var policy = _policyMapper.Map(reader);
            policy.Elements = await LoadElementsAsync(hash);

            return policy;
        }

        public async Task<IReadOnlyList<AgentPolicyEntity>> GetAllPoliciesAsync()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Hash FROM Policies";
            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<AgentPolicyEntity>();

            while (await reader.ReadAsync())
            {
                var hash = reader.GetString(0);
                var policy = await GetPolicyByHashAsync(hash);
                if (policy != null)
                    list.Add(policy);
            }

            return list;
        }

        public async Task SaveOrUpdatePolicyAsync(AgentPolicyEntity policy)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO Policies (Hash, Name, Scope, RegistryKey, ValueName, EnabledValue, DisabledValue, SourceRevision)
VALUES (@hash, @name, @scope, @rk, @vn, @en, @dis, @rev)
ON CONFLICT(Hash) DO UPDATE SET
    Name = excluded.Name,
    Scope = excluded.Scope,
    RegistryKey = excluded.RegistryKey,
    ValueName = excluded.ValueName,
    EnabledValue = excluded.EnabledValue,
    DisabledValue = excluded.DisabledValue,
    SourceRevision = excluded.SourceRevision;
";
            cmd.Parameters.AddWithValue("@hash", policy.Hash);
            cmd.Parameters.AddWithValue("@name", policy.Name);
            cmd.Parameters.AddWithValue("@scope", policy.Scope);
            cmd.Parameters.AddWithValue("@rk", policy.RegistryKey);
            cmd.Parameters.AddWithValue("@vn", policy.ValueName);
            cmd.Parameters.AddWithValue("@en", (object?)policy.EnabledValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dis", (object?)policy.DisabledValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@rev", policy.SourceRevision);

            await cmd.ExecuteNonQueryAsync();

            foreach (var el in policy.Elements)
            {
                using var elCmd = _conn.CreateCommand();
                elCmd.CommandText = @"
INSERT INTO PolicyElements (PolicyHash, ElementId, Type, ValueName, MaxLength, Required, ClientExtension)
VALUES (@hash, @eid, @type, @vn, @max, @req, @ext)
ON CONFLICT(Id) DO UPDATE SET
    Type = excluded.Type,
    ValueName = excluded.ValueName,
    MaxLength = excluded.MaxLength,
    Required = excluded.Required,
    ClientExtension = excluded.ClientExtension;
";
                elCmd.Parameters.AddWithValue("@hash", policy.Hash);
                elCmd.Parameters.AddWithValue("@eid", el.ElementId);
                elCmd.Parameters.AddWithValue("@type", el.Type);
                elCmd.Parameters.AddWithValue("@vn", (object?)el.ValueName ?? DBNull.Value);
                elCmd.Parameters.AddWithValue("@max", (object?)el.MaxLength ?? DBNull.Value);
                elCmd.Parameters.AddWithValue("@req", el.Required);
                elCmd.Parameters.AddWithValue("@ext", (object?)el.ClientExtension ?? DBNull.Value);

                await elCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task SaveComplianceAsync(AgentPolicyComplianceEntity compliance)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO AgentPolicyCompliance (PolicyHash, UserSid, State, ActualValue, LastCheckedAt)
VALUES (@hash, @sid, @state, @val, @ts)
ON CONFLICT(PolicyHash, UserSid) DO UPDATE SET
    State = excluded.State,
    ActualValue = excluded.ActualValue,
    LastCheckedAt = excluded.LastCheckedAt;
";
            cmd.Parameters.AddWithValue("@hash", compliance.PolicyHash);
            cmd.Parameters.AddWithValue("@sid", (object?)compliance.UserSid ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@state", compliance.State);
            cmd.Parameters.AddWithValue("@val", (object?)compliance.ActualValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ts", compliance.LastCheckedAt);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IReadOnlyList<AgentPolicyComplianceEntity>> GetComplianceForPolicyAsync(string policyHash)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM AgentPolicyCompliance WHERE PolicyHash = @hash";
            cmd.Parameters.AddWithValue("@hash", policyHash);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<AgentPolicyComplianceEntity>();
            while (await reader.ReadAsync())
            {
                list.Add(_complianceMapper.Map(reader));
            }

            return list;
        }

        private async Task<List<AgentPolicyElementEntity>> LoadElementsAsync(string policyHash)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM PolicyElements WHERE PolicyHash = @hash";
            cmd.Parameters.AddWithValue("@hash", policyHash);

            using var reader = await cmd.ExecuteReaderAsync();
            var elements = new List<AgentPolicyElementEntity>();
            while (await reader.ReadAsync())
            {
                elements.Add(_elementMapper.Map(reader));
            }

            return elements;
        }
    }
}
