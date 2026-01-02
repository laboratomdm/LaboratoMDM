using Grpc.Core;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Services.Abstractions;

namespace LaboratoMDM.Mesh.Master.Grpc;

public sealed class PolicyCatalogServiceImpl : PolicyCatalogService.PolicyCatalogServiceBase
{
    private readonly IPolicyQueryService _policyQueryService;

    public PolicyCatalogServiceImpl(IPolicyQueryService policyQueryService)
    {
        _policyQueryService = policyQueryService;
    }

    // Метод ListPolicies (для конкретного ADMX файла)
    public override async Task<ListPoliciesResponse> ListPolicies(
        ListPoliciesRequest request,
        ServerCallContext context)
    {
        throw new NotImplementedException();
    }

    // Метод GetPolicy по hash
    public override async Task<PolicyDescriptor> GetPolicy(
        GetPolicyRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.PolicyHash))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "PolicyHash is required"));

        var policy = await _policyQueryService.GetByHash(request.PolicyHash);
        if (policy == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Policy with hash {request.PolicyHash} not found"));

        return MapPolicy(policy);
    }

    // Новый метод для группировки по Scope
    public override async Task<ListPoliciesGroupedByScopeResponse> ListPoliciesGroupedByScope(
        ListPoliciesGroupedByScopeRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrEmpty(request.LangCode))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "LangCode is required"));

        var groupsDto = await _policyQueryService.GetPoliciesGroupedByScope(request.LangCode);

        var response = new ListPoliciesGroupedByScopeResponse();
        foreach (var group in groupsDto)
        {
            var protoGroup = new PolicyGroup
            {
                Scope = group.Scope
            };
            protoGroup.Policies.AddRange(group.Policies.Select(p => new PolicySummary
            {
                Id = p.Id,
                Name = p.Name ?? string.Empty,
                DisplayName = p.DisplayName ?? string.Empty
            }));

            response.Groups.Add(protoGroup);
        }

        return response;
    }

    // Вспомогательный метод маппинга PolicyEntity -> PolicyDescriptor
    private static PolicyDescriptor MapPolicy(PolicyEntity p)
    {
        var descriptor = new PolicyDescriptor
        {
            PolicyHash = p.Hash,
            Name = p.Name,
            Scope = p.Scope switch
            {
                Core.Models.Policy.PolicyScope.User => PolicyScope.User,
                Core.Models.Policy.PolicyScope.Machine => PolicyScope.Machine,
                Core.Models.Policy.PolicyScope.Both => PolicyScope.Both,
                _ => PolicyScope.None
            },
            RegistryKey = p.RegistryKey ?? string.Empty,
            ValueName = p.ValueName ?? string.Empty,
            EnabledValue = p.EnabledValue,
            DisabledValue = p.DisabledValue,
            ParentCategory = p.ParentCategory ?? string.Empty,
            SupportedOnRef = p.SupportedOnRef ?? string.Empty,
            PresentationRef = p.PresentationRef ?? string.Empty,
        };

        foreach (var e in p.Elements)
        {
            descriptor.Elements.Add(new PolicyElement
            {
                IdName = e.IdName,
                Type = e.Type,
                ValueName = e.ValueName ?? string.Empty,
                MaxLength = e.MaxLength ?? 0,
                Required = e.Required,
                ClientExtension = e.ClientExtension ?? string.Empty
            });
        }

        descriptor.RequiredCapabilities.AddRange(p.Capabilities.Select(c => c.Capability));
        descriptor.RequiredHardware.AddRange(p.HardwareRequirements.Select(h => h.HardwareFeature));

        return descriptor;
    }
}
