using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.Mesh.Agent.Services
{
    public sealed record PolicyTarget(
        string? UserSid,
        string? GroupName,
        bool IsMachine)
    {
        public static PolicyTarget ForMachine() =>
            new(null, null, true);

        public static PolicyTarget ForUser(string sid) =>
            new(sid, null, false);

        public static PolicyTarget ForGroup(string group) =>
            new(null, group, false);
    }

    public interface IPolicyCommandService
    {
        Task ApplyAsync(
            string policyHash,
            PolicySelection selection,
            PolicyTarget target,
            CancellationToken ct);

        Task RemoveAsync(
            string policyHash,
            PolicyTarget target,
            CancellationToken ct);
    }
}