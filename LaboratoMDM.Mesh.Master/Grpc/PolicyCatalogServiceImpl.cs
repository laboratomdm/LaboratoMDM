using Google.Protobuf.WellKnownTypes;
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

    // Метод для группировки по Scope
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
                DisplayName = p.DisplayName ?? string.Empty,
                ExplainText = p.ExplainText ?? string.Empty
            }));

            response.Groups.Add(protoGroup);
        }

        return response;
    }

    public override async Task<PolicyDetails> GetPolicyDetails(
        GetPolicyDetailsRequest request,
        ServerCallContext context)
    {
        if (request.PolicyId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "PolicyId is required"));

        if (string.IsNullOrEmpty(request.LangCode))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "LangCode is required"));

        var details = await _policyQueryService
            .GetPolicyDetailsView(request.PolicyId, request.LangCode);

        if (details == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Policy not found"));

        // Policy
        var response = new PolicyDetails
        {
            Policy = new PolicyDetailsPolicy
            {
                Id = details.Policy.Id,
                Name = details.Policy.Name,
                Hash = details.Policy.Hash,
                Scope = details.Policy.Scope,
                ParentCategoryRef = details.Policy.ParentCategoryRef,
                SupportedOnRef = details.Policy.SupportedOnRef,
                ClientExtension = details.Policy.ClientExtension
            }
        };

        // Presentation (optional)
        if (details.Presentation != null)
        {
            var pres = new PolicyPresentation
            {
                Id = details.Presentation.Id,
                PresentationId = details.Presentation.PresentationId,
                AdmlFile = details.Presentation.AdmlFile
            };

            pres.Elements.AddRange(details.Presentation.Elements.Select(e =>
                new PolicyPresentationElement
                {
                    Id = e.Id,
                    Type = e.Type,
                    RefId = e.RefId,
                    ParentElementId = e.ParentElementId,
                    DefaultValue = e.DefaultValue,
                    Text = e.Text
                }));

            response.Presentation = pres;
        }

        // Policy elements
        foreach (var pe in details.PolicyElements)
        {
            var protoElement = new PolicyDetailsElement
            {
                Id = pe.Id,
                ElementId = pe.ElementId,
                Type = pe.Type
            };

            if (pe.ValueName != null)
                protoElement.ValueName = pe.ValueName;

            if (pe.RegistryKey != null)
                protoElement.RegistryKey = pe.RegistryKey;

            if (pe.Required.HasValue)
                protoElement.Required = pe.Required.Value;

            if (pe.MaxLength.HasValue)
                protoElement.MaxLength = pe.MaxLength.Value;

            if (pe.MaxStrings != null)
                protoElement.MaxStrings = pe.MaxStrings;

            if (pe.Expandable.HasValue)
                protoElement.Expandable = pe.Expandable.Value;

            if (pe.MinValue.HasValue)
                protoElement.MinValue = pe.MinValue.Value;

            if (pe.MaxValue.HasValue)
                protoElement.MaxValue = pe.MaxValue.Value;

            if (pe.ValuePrefix != null)
                protoElement.ValuePrefix = pe.ValuePrefix;

            if (pe.ExplicitValue.HasValue)
                protoElement.ExplicitValue = pe.ExplicitValue.Value;

            if (pe.Additive.HasValue)
                protoElement.Additive = pe.Additive.Value;


            if (pe.Items != null)
            {
                protoElement.Items.AddRange(pe.Items.Select(i =>
                    new PolicyDetailsElementItem
                    {
                        Id = i.Id,
                        Name = i.Name,
                        ParentType = i.ParentType,
                        Type = i.Type,
                        ValueType = i.ValueType,
                        ValueName = i.ValueName,
                        Required = i.Required,
                        ParentId = i.ParentId,
                        DisplayName = i.DisplayName
                    }));
            }

            response.PolicyElements.Add(protoElement);
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
