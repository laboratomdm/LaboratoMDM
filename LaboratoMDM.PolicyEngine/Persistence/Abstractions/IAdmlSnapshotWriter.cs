using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Persistence.Abstractions
{
    public interface IAdmlSnapshotWriter
    {
        Task SaveSnapshot(AdmlSnapshot snapshot);
    }
}