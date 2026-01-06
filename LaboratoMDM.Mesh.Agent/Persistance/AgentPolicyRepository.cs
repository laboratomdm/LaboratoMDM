#nullable enable

using LaboratoMDM.Mesh.Agent.Domain;
using LaboratoMDM.Mesh.Agent.Persistance.Abstractions;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.Mesh.Agent.Persistance
{
    public sealed class AgentPolicyRepository : IAgentPolicyRepository
    {
        private readonly SqliteConnection _conn;
        private readonly IEntityMapper<AgentPolicyComplianceEntity> _complianceMapper;

        public AgentPolicyRepository(
            SqliteConnection conn,
            IEntityMapper<AgentPolicyComplianceEntity> complianceMapper)
        {
            _conn = conn;
            _complianceMapper = complianceMapper;
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

        /// <summary>
        /// Возвращает последнюю установленную ревизию политик.
        /// Если таблицы Versions нет, возвращает 0.
        /// </summary>
        public async Task<long> GetLastInstalledRevisionAsync()
        {
            using var checkCmd = _conn.CreateCommand();
            checkCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Versions';";
            var exists = await checkCmd.ExecuteScalarAsync() != null;
            if (!exists)
                return 0;

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(MAX(SourceRevision), 0) FROM Versions;";
            var result = await cmd.ExecuteScalarAsync();

            return result != null && result != DBNull.Value
                ? Convert.ToInt64(result)
                : 0;
        }
    }
}
