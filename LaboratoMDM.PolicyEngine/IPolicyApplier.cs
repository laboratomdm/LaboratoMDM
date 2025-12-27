using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine
{
    public sealed class PolicyExecutionContext
    {
        public bool IsMachine { get; init; }

        /// <summary>
        /// Применить к конкретному пользователю
        /// </summary>
        public string? UserSid { get; init; }

        /// <summary>
        /// Применить ко всем пользователям группы
        /// </summary>
        public string? UserGroup { get; init; }
    }

    public interface IPolicyApplier
    {
        void Apply(PolicyApplicationPlan plan, PolicyExecutionContext context);
        bool IsApplied(PolicyApplicationPlan plan, PolicyExecutionContext context);
    }
}