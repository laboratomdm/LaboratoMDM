using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Services.Abstractions
{
    public interface IAdmxImportService
    {
        Task<AdmxFileEntity> ImportAsync(
            AdmxImportModel model,
            CancellationToken ct = default);
    }
}
