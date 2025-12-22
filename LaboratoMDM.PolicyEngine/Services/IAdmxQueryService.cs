using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Services
{
    public interface IAdmxQueryService
    {
        Task<IReadOnlyList<AdmxSnapshot>> GetAllSnapshotsAsync();
        Task<AdmxSnapshot> GetSnapshotAsync(int admxFileId);
        Task<AdmxSnapshot> GetSnapshotByHashAsync(string hash);
    }
}