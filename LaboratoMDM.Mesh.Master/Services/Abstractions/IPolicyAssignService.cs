using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;

namespace LaboratoMDM.Mesh.Master.Services.Abstractions;

public interface IPolicyAssignService
{
    Task AssignPolicyAsync(
        string policyHash,
        PolicyTarget target,
        PolicySelection selection);

    Task RemovePolicyAsync(
        string policyHash,
        PolicyTarget target);
}
