#nullable enable

using Grpc.Core;
using Laborato.Mesh;
using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.Mesh.Agent.Services;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace LaboratoMDM.Mesh.Agent.Grpc
{
    [SupportedOSPlatform("windows")]
    public sealed class PolicyAgentServiceImpl(
        IPolicyCommandService commandService,
        ILogger<PolicyAgentServiceImpl> logger) : MeshService.MeshServiceBase
    {
        public override async Task<ApplyPolicyResponse> ApplyPolicy(ApplyPolicyRequest request, ServerCallContext context)
        {
            try
            {
                var target = request.TargetCase switch
                {
                    ApplyPolicyRequest.TargetOneofCase.MachineScope => new PolicyTarget(null, null, true),
                    ApplyPolicyRequest.TargetOneofCase.UserSid => new PolicyTarget(request.UserSid, null, false),
                    ApplyPolicyRequest.TargetOneofCase.GroupName => new PolicyTarget(null, request.GroupName, false),
                    _ => throw new InvalidOperationException("Invalid policy target")
                };

                var selection = MapSelection( request.Selection );

                await commandService.ApplyAsync(request.PolicyHash, selection, target, context.CancellationToken);

                return new ApplyPolicyResponse
                {
                    Success = true,
                    Message = "Policy applied successfully"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply policy {Hash}", request.PolicyHash);
                return new ApplyPolicyResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public override async Task<RemovePolicyResponse> RemovePolicy(RemovePolicyRequest request, ServerCallContext context)
        {
            try
            {
                var target = request.TargetCase switch
                {
                    RemovePolicyRequest.TargetOneofCase.MachineScope => new PolicyTarget(null, null, true),
                    RemovePolicyRequest.TargetOneofCase.UserSid => new PolicyTarget(request.UserSid, null, false),
                    RemovePolicyRequest.TargetOneofCase.GroupName => new PolicyTarget(null, request.GroupName, false),
                    _ => throw new InvalidOperationException("Invalid policy target")
                };

                await commandService.RemoveAsync(request.PolicyHash, target, context.CancellationToken);

                return new RemovePolicyResponse
                {
                    Success = true,
                    Message = "Policy removed"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove policy {Hash}", request.PolicyHash);
                return new RemovePolicyResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private static Core.Models.Policy.PolicySelection MapSelection(Laborato.Mesh.PolicySelection sel)
        {
            return new Core.Models.Policy.PolicySelection
            {
                Value = sel.Value,
                ListKeys = sel.ListKeys.ToList(),
                Elements = sel.Elements.Select(MapElement).ToList()
            };
        }

        private static Core.Models.Policy.PolicyElementSelection MapElement(Laborato.Mesh.PolicyElementSelection e)
        {
            return new Core.Models.Policy.PolicyElementSelection
            {
                IdName = e.IdName,
                Value = e.Value,
                Childs = e.Childs.Select(MapElementItem).ToList()
            };
        }

        private static Core.Models.Policy.PolicyElementItemSelection MapElementItem(Laborato.Mesh.PolicyElementItemSelection e)
        {
            return new Core.Models.Policy.PolicyElementItemSelection
            {
                IdName = e.IdName,
                Value = e.Value,
                Childs = e.Childs.Select(MapElementItem).ToList()
            };
        }
    }
}
