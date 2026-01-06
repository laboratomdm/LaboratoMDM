#nullable enable
using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.Mesh.Agent.Domain;
using LaboratoMDM.NodeEngine;
using LaboratoMDM.PolicyEngine;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace LaboratoMDM.Mesh.Agent.Services
{
    public sealed class PolicyCommandService : IPolicyCommandService
    {
        private readonly IUserCollector _userCollector;
        private readonly IPolicyPlanner _planner;
        private readonly IPolicyApplier _applier;
        private readonly IPolicyApplicabilityChecker<PolicyDefinition> _checker;
        private readonly IAgentPolicyService _agentPolicyService;
        private readonly IPolicyQueryService _policyQuery;
        private readonly ILogger<PolicyCommandService> _logger;

        public PolicyCommandService(
            IUserCollector userCollector,
            IPolicyPlanner planner,
            IPolicyApplier applier,
            IPolicyApplicabilityChecker<PolicyDefinition> checker,
            IAgentPolicyService agentPolicyService,
            IPolicyQueryService policyQuery,
            ILogger<PolicyCommandService> logger)
        {
            _userCollector = userCollector;
            _planner = planner;
            _applier = applier;
            _checker = checker;
            _agentPolicyService = agentPolicyService;
            _policyQuery = policyQuery;
            _logger = logger;
        }

        /// <summary>
        /// Применяет политику с переданными выбранными значениями
        /// </summary>
        public async Task ApplyAsync(
            string policyHash,
            PolicySelection selection,
            PolicyTarget target,
            CancellationToken ct)
        {
            var policyEntity = await _policyQuery.GetByHash(policyHash)
                ?? throw new InvalidOperationException(
                    $"Policy {policyHash} not synced on agent");

            var policy = PolicyMapper.ToDefinition(policyEntity);

            var executionContext = ResolveTarget(target);

            // Проверка применимости
            var applicability = _checker.Check(policy, new PolicyEvaluationContext());
            if (applicability.Status != PolicyApplicabilityStatus.Applicable)
                throw new InvalidOperationException(
                    $"Policy not applicable: {applicability.Reason}");

            // Строим план с переданными значениями
            var plan = _planner.BuildPlan(policy, selection);

            // Применяем
            _applier.Apply(plan, executionContext);

            await SaveComplianceAsync(policy.Hash, target, "Applied");
        }

        /// <summary>
        /// Удаляет/откатывает политику
        /// </summary>
        public async Task RemoveAsync(
            string policyHash,
            PolicyTarget target,
            CancellationToken ct)
        {
            var policyEntity = await _policyQuery.GetByHash(policyHash)
                ?? throw new InvalidOperationException(
                    $"Policy {policyHash} not found");

            var policy = PolicyMapper.ToDefinition(policyEntity);
            var executionContext = ResolveTarget(target);

            // Строим план отката
            var plan = _planner.BuildPlan(policy, new PolicySelection() { /* надо договориться как будем удалять комплексную примененную политику */ });

            if (!_applier.IsApplied(plan, executionContext))
                throw new InvalidOperationException("Policy not applied");

            _applier.Apply(plan, executionContext);

            await SaveComplianceAsync(policy.Hash, target, "NotApplied");
        }

        /// <summary>
        /// Конвертирует PolicyTarget в PolicyExecutionContext
        /// </summary>
        private PolicyExecutionContext ResolveTarget(PolicyTarget target)
        {
            if (target.IsMachine)
                return new PolicyExecutionContext { IsMachine = true };

            if (target.UserSid != null)
            {
                if (!_userCollector.GetAllUsers()
                    .Any(u => u.Sid == target.UserSid))
                    throw new InvalidOperationException(
                        $"User {target.UserSid} not found");

                return new PolicyExecutionContext
                {
                    UserSid = target.UserSid
                };
            }

            if (target.GroupName != null)
                return new PolicyExecutionContext
                {
                    UserGroup = target.GroupName
                };

            throw new InvalidOperationException("Invalid target");
        }

        /// <summary>
        /// Сохраняет состояние compliance
        /// </summary>
        private async Task SaveComplianceAsync(
            string hash,
            PolicyTarget target,
            string state)
        {
            if (target.UserSid != null)
            {
                await _agentPolicyService.SaveComplianceAsync(
                    new AgentPolicyComplianceEntity
                    {
                        PolicyHash = hash,
                        UserSid = target.UserSid,
                        State = state,
                        LastCheckedAt = DateTime.UtcNow
                    });
            }
            else
            {
                await _agentPolicyService.SaveComplianceAsync(
                    new AgentPolicyComplianceEntity
                    {
                        PolicyHash = hash,
                        State = state,
                        LastCheckedAt = DateTime.UtcNow
                    });
            }
        }
    }
}
