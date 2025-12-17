using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Implementations;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    [SupportedOSPlatform("windows")]
    public class PolicyPlannerTests
    {
        private readonly PolicyPlanner _planner = new();

        [Fact]
        public void BuildPlan_Should_Create_Delete_Operation_When_Disabling_Policy_Without_DisabledValue()
        {
            var policy = new PolicyDefinition
            {
                Name = "TestPolicy",
                Scope = PolicyScope.Machine,
                RegistryKey = @"Software\Test",
                ValueName = "Enabled",
                EnabledValue = 1,
                DisabledValue = null
            };

            var plan = _planner.BuildPlan(policy, enable: false);

            Assert.Equal("TestPolicy", plan.PolicyName);
            Assert.Single(plan.Operations);

            var op = plan.Operations.First();
            Assert.True(op.Delete);

            Assert.Equal(@"Software\Test", op.Key);
            Assert.Equal("Enabled", op.ValueName);
            Assert.Null(op.Value);
        }

        [Fact]
        public void BuildPlan_Should_Create_Set_Operation_When_Enabling_Policy()
        {
            var policy = new PolicyDefinition
            {
                Name = "TestPolicy",
                Scope = PolicyScope.User,
                RegistryKey = @"Software\Test",
                ValueName = "Enabled",
                EnabledValue = 1,
                DisabledValue = 0
            };

            var plan = _planner.BuildPlan(policy, enable: true);

            Assert.Single(plan.Operations);

            var op = plan.Operations.First();
            Assert.False(op.Delete);

            Assert.NotNull(op.Value);
            Assert.Equal(RegistryValueKind.DWord, op.ValueKind);
            Assert.Equal(PolicyScope.User, op.Scope);
        }

        [Fact]
        public void BuildPlan_Should_Handle_ListKeys_When_Enabling()
        {
            var policy = new PolicyDefinition
            {
                Name = "ListPolicy",
                Scope = PolicyScope.Machine,
                ListKeys = [@"Software\List1", @"Software\List2"]
            };

            var plan = _planner.BuildPlan(policy, enable: true);

            Assert.True(plan.Operations.Count == 2);
            Assert.True(plan.Operations.All(o => o.Value?.Equals(1) ?? false));
            Assert.True(plan.Operations.All(o => !o.Delete));
        }

        [Fact]
        public void BuildPlan_Should_Handle_ListKeys_When_Disabling()
        {
            var policy = new PolicyDefinition
            {
                Name = "ListPolicy",
                Scope = PolicyScope.Machine,
                ListKeys = [@"Software\List1", @"Software\List2"]
            };

            var plan = _planner.BuildPlan(policy, enable: false);

            Assert.True(plan.Operations.Count == 2);
            Assert.False(plan.Operations.All(o => !o.Delete));
            Assert.False(plan.Operations.All(o => o.Value?.Equals(1) ?? false));
        }
    }
}