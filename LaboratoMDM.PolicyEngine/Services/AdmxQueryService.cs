using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Services.Abstractions;

namespace LaboratoMDM.PolicyEngine.Services
{
    public sealed class AdmxQueryService : IAdmxQueryService
    {
        private readonly IAdmxRepository _repository;

        public AdmxQueryService(IAdmxRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<AdmxSnapshot>> GetAllSnapshotsAsync()
            => _repository.LoadAllSnapshots();

        public Task<AdmxSnapshot> GetSnapshotAsync(int admxFileId)
            => _repository.LoadSnapshot(admxFileId);

        public async Task<AdmxSnapshot> GetSnapshotByHashAsync(string hash)
        {
            var admxFile = await _repository.GetByHash(hash);
            if (admxFile == null)
                throw new InvalidOperationException($"Has no loaded ADMX file with hash: {hash}");
            return await _repository.LoadSnapshot(admxFile.Id);
        }
    }

}
