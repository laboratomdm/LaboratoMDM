using LaboratoMDM.Mesh.Agent.Domain;
using LaboratoMDM.Mesh.Agent.Persistance.Abstractions;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.Mesh.Agent.Services
{
    public sealed class AgentPolicyService : IAgentPolicyService
    {
        private readonly IAgentPolicyRepository _repository;
        private readonly ILogger<AgentPolicyService> _logger;

        public AgentPolicyService(
            IAgentPolicyRepository repository,
            ILogger<AgentPolicyService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<AgentPolicyEntity?> GetPolicyAsync(string hash)
        {
            _logger.LogInformation("Fetching policy with hash {Hash}", hash);
            var policy = await _repository.GetPolicyByHashAsync(hash);
            if (policy == null)
                _logger.LogWarning("Policy with hash {Hash} not found", hash);
            return policy;
        }

        public async Task<IReadOnlyList<AgentPolicyEntity>> GetAllPoliciesAsync()
        {
            _logger.LogInformation("Fetching all policies");
            var policies = await _repository.GetAllPoliciesAsync();
            _logger.LogInformation("Fetched {Count} policies", policies.Count);
            return policies;
        }

        public async Task SaveOrUpdatePolicyAsync(AgentPolicyEntity policy)
        {
            _logger.LogInformation("Saving/updating policy {Hash} - {Name}", policy.Hash, policy.Name);
            await _repository.SaveOrUpdatePolicyAsync(policy);
            _logger.LogInformation("Policy {Hash} saved/updated successfully", policy.Hash);
        }

        public async Task SaveComplianceAsync(AgentPolicyComplianceEntity compliance)
        {
            _logger.LogInformation("Saving compliance for policy {Hash} user {UserSid}",
                compliance.PolicyHash, compliance.UserSid ?? "<machine>");
            await _repository.SaveComplianceAsync(compliance);
            _logger.LogInformation("Compliance for policy {Hash} saved successfully", compliance.PolicyHash);
        }

        public async Task<IReadOnlyList<AgentPolicyComplianceEntity>> GetComplianceForPolicyAsync(string policyHash)
        {
            _logger.LogInformation("Fetching compliance for policy {Hash}", policyHash);
            var list = await _repository.GetComplianceForPolicyAsync(policyHash);
            _logger.LogInformation("Fetched {Count} compliance entries for policy {Hash}", list.Count, policyHash);
            return list;
        }
    }
}
