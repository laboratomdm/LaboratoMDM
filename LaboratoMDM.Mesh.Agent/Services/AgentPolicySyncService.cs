#nullable enable
using LaboratoMDM.Mesh.Agent.Persistance;

namespace LaboratoMDM.Mesh.Agent.Services
{
    /// <summary>
    /// Бизнес-логика синхронизации payload на агенте
    /// </summary>
    public sealed class AgentPolicySyncService
    {
        private readonly AgentPolicySyncRepository _repository;

        public AgentPolicySyncService(AgentPolicySyncRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Применяет новый payload SQLite.
        /// Проверяет SHA256 и integrity.
        /// </summary>
        public Task ApplyPayloadAsync(string payloadPath, string expectedSha256, long revision, CancellationToken ct = default)
        {
            if (!File.Exists(payloadPath))
                throw new FileNotFoundException("Payload not found", payloadPath);

            // Вся логика проверки SHA и замены базы реализована в репозитории
            return _repository.ApplyPayloadAsync(payloadPath, expectedSha256, revision, ct);
        }
    }
}
