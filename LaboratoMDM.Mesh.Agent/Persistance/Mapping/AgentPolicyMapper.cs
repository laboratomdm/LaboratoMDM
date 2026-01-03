using LaboratoMDM.Mesh.Agent.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.Mesh.Agent.Persistance.Mapping
{
    public sealed class AgentPolicyMapper : IEntityMapper<AgentPolicyEntity>
    {
        public AgentPolicyEntity Map(SqliteDataReader r) => new AgentPolicyEntity
        {
            Hash = r.GetString(r.GetOrdinal("Hash")),
            Name = r.GetString(r.GetOrdinal("Name")),
            ScopeString = r.GetString(r.GetOrdinal("Scope")),
            RegistryKey = r.GetString(r.GetOrdinal("RegistryKey")),
            ValueName = r.GetString(r.GetOrdinal("ValueName")),
            EnabledValue = r.IsDBNull(r.GetOrdinal("EnabledValue")) ? null : r.GetString(r.GetOrdinal("EnabledValue")),
            DisabledValue = r.IsDBNull(r.GetOrdinal("DisabledValue")) ? null : r.GetString(r.GetOrdinal("DisabledValue")),
            SourceRevision = r.GetInt32(r.GetOrdinal("SourceRevision")),
            Elements = new List<AgentPolicyElementEntity>()
        };
    }

    public sealed class AgentPolicyElementMapper : IEntityMapper<AgentPolicyElementEntity>
    {
        public AgentPolicyElementEntity Map(SqliteDataReader r) => new AgentPolicyElementEntity
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            PolicyHash = r.GetString(r.GetOrdinal("PolicyHash")),
            ElementId = r.GetString(r.GetOrdinal("ElementId")),
            Type = r.GetString(r.GetOrdinal("Type")),
            ValueName = r.IsDBNull(r.GetOrdinal("ValueName")) ? null : r.GetString(r.GetOrdinal("ValueName")),
            MaxLength = r.IsDBNull(r.GetOrdinal("MaxLength")) ? null : r.GetInt32(r.GetOrdinal("MaxLength")),
            Required = r.GetBoolean(r.GetOrdinal("Required")),
            ClientExtension = r.IsDBNull(r.GetOrdinal("ClientExtension")) ? null : r.GetString(r.GetOrdinal("ClientExtension"))
        };
    }

    public sealed class AgentPolicyComplianceMapper : IEntityMapper<AgentPolicyComplianceEntity>
    {
        public AgentPolicyComplianceEntity Map(SqliteDataReader r) => new AgentPolicyComplianceEntity
        {
            PolicyHash = r.GetString(r.GetOrdinal("PolicyHash")),
            UserSid = r.IsDBNull(r.GetOrdinal("UserSid")) ? null : r.GetString(r.GetOrdinal("UserSid")),
            State = r.GetString(r.GetOrdinal("State")),
            ActualValue = r.IsDBNull(r.GetOrdinal("ActualValue")) ? null : r.GetString(r.GetOrdinal("ActualValue")),
            LastCheckedAt = r.GetDateTime(r.GetOrdinal("LastCheckedAt"))
        };
    }
}