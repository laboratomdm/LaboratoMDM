using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;

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

            foreach (var ns in model.Namespaces)
            {
                ns.AdmxFileId = admx.Id;
                await _metadataRepo.CreateNamespaceIfNotExists(ns);
            }

            foreach (var category in model.Categories)
            {
                category.AdmxFileId = admx.Id;
                await _metadataRepo.CreateCategoryIfNotExists(category);
            }

            foreach (var policy in model.Policies)
            {
                var created = await _policyRepo.CreateIfNotExists(policy);
                await _policyRepo.LinkPolicyToAdmx(created.Id, admx.Id);
            }

            return admx;
        }
    }

}
