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
        public void BuildPlan_Should_Create_Delete_Operation_When_Disabling_Legacy_Policy()
        {
            var policy = new PolicyDefinition
            {
                Name = "TestPolicy",
                Scope = PolicyScope.Machine,
                RegistryKey = @"Software\Test",
                ValueName = "Enabled",
                Elements = []
            };

            var selection = new PolicySelection
            {
                Value = null // имитируем отключение без DisabledValue
            };

            var plan = _planner.BuildPlan(policy, selection);

            var op = Assert.Single(plan.Operations);
            Assert.True(op.Delete);
            Assert.Equal(@"Software\Test", op.Key);
            Assert.Equal("Enabled", op.ValueName);
            Assert.Null(op.Value);
        }

        [Fact]
        public void BuildPlan_Should_Create_Set_Operation_For_Legacy_Policy()
        {
            var policy = new PolicyDefinition
            {
                Name = "TestPolicy",
                Scope = PolicyScope.User,
                RegistryKey = @"Software\Test",
                ValueName = "Enabled",
                Elements = []
            };

            var selection = new PolicySelection
            {
                Value = "1"
            };

            var plan = _planner.BuildPlan(policy, selection);

            var op = Assert.Single(plan.Operations);
            Assert.False(op.Delete);
            Assert.Equal("1", op.Value);
            Assert.Equal(RegistryValueKind.DWord, op.ValueKind);
            Assert.Equal(PolicyScope.User, op.Scope);
        }

        [Fact]
        public void BuildPlan_Should_Handle_ListKeys()
        {
            var policy = new PolicyDefinition
            {
                Name = "ListPolicy",
                Scope = PolicyScope.Machine,
                ListKeys = [@"Software\List1", @"Software\List2"]
            };

            var selection = new PolicySelection
            {
                ListKeys = [@"Software\List1", @"Software\List2"]
            };

            var plan = _planner.BuildPlan(policy, selection);

            Assert.Equal(2, plan.Operations.Count);
            Assert.All(plan.Operations, o =>
            {
                Assert.False(o.Delete);
                Assert.Equal(1, o.Value);
            });
        }

        [Fact]
        public void BuildPlan_Should_Create_Operations_For_PolicyElements()
        {
            var policy = new PolicyDefinition
            {
                Name = "ElementPolicy",
                Scope = PolicyScope.Machine,
                RegistryKey = @"Software\Test",
                Elements =
                [
                    new PolicyElementDefinition { IdName = "Elem1", ValueName = "Value1", Type = PolicyElementType.BOOLEAN },
                    new PolicyElementDefinition { IdName = "Elem2", ValueName = "Value2", Type = PolicyElementType.DECIMAL }
                ]
            };

            var selection = new PolicySelection
            {
                Elements =
                [
                    new PolicyElementSelection { IdName = "Elem1", Value = "1" },
                    new PolicyElementSelection { IdName = "Elem2", Value = "42" }
                ]
            };

            var plan = _planner.BuildPlan(policy, selection);

            Assert.Equal(2, plan.Operations.Count);

            var op1 = plan.Operations.First(o => o.ValueName == "Value1");
            Assert.Equal("1", op1.Value);

            var op2 = plan.Operations.First(o => o.ValueName == "Value2");
            Assert.Equal("42", op2.Value);
        }

        [Fact]
        public void BuildPlan_Should_Handle_ENUM_Element()
        {
            var policy = new PolicyDefinition
            {
                Name = "EnumPolicy",
                Scope = PolicyScope.Machine,
                RegistryKey = @"Software\EnumTest",
                Elements =
                [
                    new PolicyElementDefinition
                    {
                        IdName = "Color",
                        Type = PolicyElementType.ENUM,
                        Childs =
                        [
                            new PolicyElementItemDefinition { IdName = "Red", ValueType = PolicyElementItemValueType.STRING },
                            new PolicyElementItemDefinition { IdName = "Blue", ValueType = PolicyElementItemValueType.STRING }
                        ]
                    }
                ]
            };

            var selection = new PolicySelection
            {
                Elements =
                [
                    new PolicyElementSelection
                    {
                        IdName = "Color",
                        Childs = [ new PolicyElementItemSelection { IdName = "Red", Value = "1" } ]
                    }
                ]
            };

            var plan = _planner.BuildPlan(policy, selection);

            var op = Assert.Single(plan.Operations);
            Assert.Equal("Red", op.ValueName);
            Assert.Equal("1", op.Value);
        }

        [Fact]
        public void BuildPlan_Should_Handle_LIST_Element_With_Multiple_Values()
        {
            var policy = new PolicyDefinition
            {
                Name = "ListElementPolicy",
                Scope = PolicyScope.Machine,
                RegistryKey = @"Software\ListElementTest",
                Elements =
                [
                    new PolicyElementDefinition
                    {
                        IdName = "Services",
                        Type = PolicyElementType.LIST,
                        Childs =
                        [
                            new PolicyElementItemDefinition { IdName = "SvcA", ValueType = PolicyElementItemValueType.STRING },
                            new PolicyElementItemDefinition { IdName = "SvcB", ValueType = PolicyElementItemValueType.STRING }
                        ]
                    }
                ]
            };

            var selection = new PolicySelection
            {
                Elements =
                [
                    new PolicyElementSelection
                    {
                        IdName = "Services",
                        Childs =
                        [
                            new PolicyElementItemSelection { IdName = "SvcA", Value = "Enabled" },
                            new PolicyElementItemSelection { IdName = "SvcB", Value = "Disabled" }
                        ]
                    }
                ]
            };

            var plan = _planner.BuildPlan(policy, selection);

            Assert.Equal(2, plan.Operations.Count);
            Assert.Contains(plan.Operations, o => o.ValueName == "SvcA" && (string)o.Value! == "Enabled");
            Assert.Contains(plan.Operations, o => o.ValueName == "SvcB" && (string)o.Value! == "Disabled");
        }
    }
}
