#nullable enable

using Grpc.Core;
using Laborato.Mesh;
using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.Mesh.Agent.Domain;
using LaboratoMDM.Mesh.Agent.Services;
using LaboratoMDM.NodeEngine;
using LaboratoMDM.PolicyEngine;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace LaboratoMDM.Mesh.Agent.Grpc
{
    [SupportedOSPlatform("windows")]
    public sealed class PolicyAgentServiceImpl(
        IUserCollector userCollector,
        IPolicyPlanner planner,
        IPolicyApplier applier,
        IPolicyApplicabilityChecker<PolicyDefinition> checker,
        IAgentPolicyService service,
        ILogger<PolicyAgentServiceImpl> logger) : MeshService.MeshServiceBase
    {
        public override async Task<ApplyPolicyResponse> ApplyPolicy(ApplyPolicyRequest request, ServerCallContext context)
        {
            logger.LogInformation(
                "ApplyPolicy started. Hash={Hash}, Enable={Enable}, Target={Target}",
                request.Policy.PolicyHash,
                request.Enable,
                request.TargetCase);

            try
            {
                var policy = MapToPolicyDefinition(request.Policy);

                // Resolve target
                PolicyExecutionContext executionContext;
                switch (request.TargetCase)
                {
                    case ApplyPolicyRequest.TargetOneofCase.MachineScope:
                        executionContext = new PolicyExecutionContext { IsMachine = true };
                        break;

                    case ApplyPolicyRequest.TargetOneofCase.UserSid:
                        if (!userCollector.GetAllUsers().Any(u => u.Sid == request.UserSid))
                            return Fail($"User with SID {request.UserSid} not found");

                        executionContext = new PolicyExecutionContext
                        {
                            UserSid = request.UserSid
                        };
                        break;

                    case ApplyPolicyRequest.TargetOneofCase.GroupName:
                        var usersInGroup = userCollector.GetAllUsers()
                            .Where(u => u.Groups.Contains(request.GroupName))
                            .ToList();

                        if (usersInGroup.Count == 0)
                            return Fail($"Group '{request.GroupName}' has no users");

                        executionContext = new PolicyExecutionContext
                        {
                            UserGroup = request.GroupName
                        };
                        break;

                    default:
                        return Fail("Invalid policy target");
                }

                // Applicability check
                var evalContext = new PolicyEvaluationContext();
                var applicability = checker.Check(policy, evalContext);

                if (applicability.Status != PolicyApplicabilityStatus.Applicable)
                {
                    logger.LogWarning(
                        "Policy {Hash} is not applicable: {Reason}",
                        policy.Hash,
                        applicability.Reason);

                    return Fail($"Policy not applicable: {applicability.Reason}");
                }

                // Ensure policy exists in agent DB
                var stored = await service.GetPolicyAsync(policy.Hash);
                if (stored == null)
                {
                    logger.LogInformation(
                        "Policy {Hash} not found locally. Creating.",
                        policy.Hash);

                    var agentEntity = AgentPolicyEntityMapper.FromPolicyDefinition(
                        policy,
                        request.Policy.Revision
                        );

                    await service.SaveOrUpdatePolicyAsync(agentEntity);
                }

                // Build plan
                var plan = planner.BuildPlan(policy, enable: request.Enable);

                logger.LogDebug(
                    "Policy plan built. Operations={Count}",
                    plan.Operations.Count);

                // Apply
                applier.Apply(plan, executionContext);

                logger.LogInformation(
                    "Policy {Hash} applied successfully",
                    policy.Hash);

                return new ApplyPolicyResponse
                {
                    Success = true,
                    Message = "Policy applied successfully"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "ApplyPolicy failed for {Hash}",
                    request.Policy.PolicyHash);

                return new ApplyPolicyResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }

            static ApplyPolicyResponse Fail(string message) =>
                new() { Success = false, Message = message };
        }

        public override async Task<RemovePolicyResponse> RemovePolicy(
            RemovePolicyRequest request,
            ServerCallContext context)
        {
            var hash = request.Policy.PolicyHash;

            logger.LogInformation("RemovePolicy requested: {Hash}", hash);

            try
            {
                // Проверяем, есть ли политика в локальной БД
                var agentPolicy = await service.GetPolicyAsync(hash);
                if (agentPolicy == null)
                {
                    logger.LogWarning(
                        "RemovePolicy failed: policy {Hash} not found on agent",
                        hash);

                    return new RemovePolicyResponse
                    {
                        Success = false,
                        Message = "Policy not found on agent"
                    };
                }

                // Определяем target
                string? userSid = null;
                string? groupName = null;
                bool isMachine = false;

                switch (request.TargetCase)
                {
                    case RemovePolicyRequest.TargetOneofCase.UserSid:
                        userSid = request.UserSid;
                        break;

                    case RemovePolicyRequest.TargetOneofCase.GroupName:
                        groupName = request.GroupName;
                        break;

                    case RemovePolicyRequest.TargetOneofCase.MachineScope:
                        isMachine = true;
                        break;

                    default:
                        return new RemovePolicyResponse
                        {
                            Success = false,
                            Message = "Invalid policy target"
                        };
                }

                // Проверяем compliance — применена ли политика
                var complianceList = await service.GetComplianceForPolicyAsync(hash);
                var compliance = complianceList.FirstOrDefault(c => c.UserSid == userSid);

                if (compliance == null || compliance.State != "Applied")
                {
                    logger.LogWarning(
                        "RemovePolicy rejected by DB state: {Hash}, state={State}",
                        hash,
                        compliance?.State ?? "None");

                    return new RemovePolicyResponse
                    {
                        Success = false,
                        Message = "Policy is not marked as applied"
                    };
                }

                // Формируем execution context
                var executionContext = new PolicyExecutionContext
                {
                    IsMachine = isMachine,
                    UserSid = userSid,
                    UserGroup = groupName
                };

                // Строим план отмены
                var policy = MapToPolicyDefinition(request.Policy);
                var plan = planner.BuildPlan(policy, enable: false);

                if (!applier.IsApplied(plan, executionContext))
                {
                    logger.LogWarning("Remove rejected: policy not applied in system");
                    return new RemovePolicyResponse
                    {
                        Success = false,
                        Message = "Policy is not applied"
                    };
                }

                // Применяем
                applier.Apply(plan, executionContext);

                if (!string.IsNullOrEmpty(groupName))
                {
                    var userSids = LocalUserResolver.GetUserSidsByGroup(groupName);
                    foreach (var sid in userSids)
                    {
                        await service.SaveComplianceAsync(new AgentPolicyComplianceEntity
                        {
                            PolicyHash = policy.Hash,
                            UserSid = sid,
                            State = "NotApplied",
                            ActualValue = null,
                            LastCheckedAt = DateTime.UtcNow
                        });
                    }
                }
                else if (!string.IsNullOrEmpty(userSid))
                {
                    // Обновляем compliance
                    await service.SaveComplianceAsync(new AgentPolicyComplianceEntity
                    {
                        PolicyHash = policy.Hash,
                        UserSid = executionContext.UserSid,
                        State = "NotApplied",
                        ActualValue = null,
                        LastCheckedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    await service.SaveComplianceAsync(new AgentPolicyComplianceEntity
                    {
                        PolicyHash = policy.Hash,
                        State = "NotApplied",
                        ActualValue = null,
                        LastCheckedAt = DateTime.UtcNow
                    });
                }

                logger.LogInformation(
                    "Policy {Hash} successfully removed (target={Target})",
                    hash,
                    isMachine ? "Machine" : userSid);

                return new RemovePolicyResponse
                {
                    Success = true,
                    Message = "Policy removed"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing policy {Hash}", hash);

                return new RemovePolicyResponse
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }


        public override async Task<SyncResponse> Sync(SyncRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
            //logger.LogInformation("Sync requested by agent {AgentId}, last known revision {Revision}",
            //    request.AgentId, request.LastKnownRevision);

            //var allPolicies = await service.GetAllPoliciesAsync();

            //// фильтруем политики по ревизии, если last_known_revision указан
            //var policiesToSend = allPolicies
            //    .Where(p => p.SourceRevision > request.LastKnownRevision)
            //    .Select(p => new PolicyDescriptor
            //    {
            //        PolicyHash = p.Hash,
            //        Name = p.Name,
            //        Scope = Enum.Parse<PolicyScope>(p.Scope),
            //        RegistryKey = p.RegistryKey,
            //        ValueName = p.ValueName,
            //        EnabledValue = p.EnabledValue ?? 0,
            //        DisabledValue = p.DisabledValue ?? 0,
            //        Revision = p.SourceRevision,
            //        Elements = p.Elements.Select(e => new PolicyElement
            //        {
            //            IdName = e.ElementId,
            //            Type = e.Type,
            //            ValueName = e.ValueName ?? "",
            //            MaxLength = e.MaxLength ?? 0,
            //            Required = e.Required,
            //            ClientExtension = e.ClientExtension ?? ""
            //        }).ToList()
            //    })
            //    .ToList();

            //// ADMX файлы: здесь для примера просто пустой список, можно добавить реальную синхронизацию файлов
            //var admxFiles = new List<AdmxFileDescriptor>();

            //logger.LogInformation("Sync: sending {PolicyCount} policies, {AdmxCount} ADMX files",
            //    policiesToSend.Count, admxFiles.Count);

            //return new SyncResponse
            //{
            //    CurrentMasterRevision = allPolicies.Max(p => p.SourceRevision),
            //    Policies = { policiesToSend },
            //    AdmxFiles = { admxFiles }
            //};
        }

        private static PolicyDefinition MapToPolicyDefinition(PolicyDescriptor d)
        {
            return new PolicyDefinition
            {
                Name = d.Name,
                Scope = d.Scope switch
                {
                    Laborato.Mesh.PolicyScope.User => Core.Models.Policy.PolicyScope.User,
                    Laborato.Mesh.PolicyScope.Machine => Core.Models.Policy.PolicyScope.Machine,
                    Laborato.Mesh.PolicyScope.Both => Core.Models.Policy.PolicyScope.Both,
                    _ => Core.Models.Policy.PolicyScope.None
                },
                RegistryKey = d.RegistryKey,
                ValueName = d.ValueName,
                EnabledValue = d.EnabledValue,
                DisabledValue = d.DisabledValue,
                SupportedOnRef = d.SupportedOnRef,
                ParentCategoryRef = d.ParentCategory,
                Elements = d.Elements.Select(e => new PolicyElementDefinition
                {
                    Type = e.Type,
                    IdName = e.IdName,
                    ValueName = string.IsNullOrWhiteSpace(e.ValueName) ? null : e.ValueName,
                    MaxLength = e.MaxLength > 0 ? e.MaxLength : null,
                    Required = e.Required,
                    ClientExtension = string.IsNullOrWhiteSpace(e.ClientExtension) ? null : e.ClientExtension
                }).ToList(),
                Hash = d.PolicyHash
            };
        }

    }
}