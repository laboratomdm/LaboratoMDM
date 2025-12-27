using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Services.Abstractions;

namespace LaboratoMDM.PolicyEngine.Services
{
    public sealed class AdmxImportService : IAdmxImportService
    {
        private readonly IAdmxRepository _admxRepo;
        private readonly IPolicyRepository _policyRepo;
        private readonly IPolicyMetadataRepository _metadataRepo;

        public AdmxImportService(
            IAdmxRepository admxRepo,
            IPolicyRepository policyRepo,
            IPolicyMetadataRepository metadataRepo)
        {
            _admxRepo = admxRepo;
            _policyRepo = policyRepo;
            _metadataRepo = metadataRepo;
        }

        public async Task<AdmxFileEntity> ImportAsync(
            AdmxImportModel model,
            CancellationToken ct = default)
        {
            var admx = await _admxRepo.CreateIfNotExists(
                model.FileName,
                model.FileHash);

            await _metadataRepo.CreateNamespacesBatch(
                admx.Id,
                model.Namespaces);

            await _metadataRepo.CreateCategoriesBatch(
                model.Categories);

            await _policyRepo.CreatePoliciesBatch(
                model.Policies);

            var storedPolicies = await _policyRepo
                .GetByHashes([.. model.Policies.Select(p => p.Hash)]);

            await _policyRepo.LinkPoliciesToAdmxBatch(
                admx.Id,
                [.. storedPolicies.Select(p => p.Id)]);

            return admx;
        }
    }

}
