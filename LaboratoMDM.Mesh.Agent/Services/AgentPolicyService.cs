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
