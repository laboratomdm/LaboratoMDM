using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Services.Abstractions;

public interface IAdmlImportService
{
    Task ImportAsync(
        string folderPath,
        CancellationToken cancellationToken = default
    );
}