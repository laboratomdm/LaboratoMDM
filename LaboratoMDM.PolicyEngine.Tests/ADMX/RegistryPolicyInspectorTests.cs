using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Implementations;
using Microsoft.Win32;
using System.Runtime.Versioning;
using Xunit;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    [SupportedOSPlatform("windows")]
    public class RegistryPolicyInspectorTests
    {
        private readonly RegistryPolicyInspector _inspector;
        private readonly string _testKey = @"Software\MDMTest";

        public RegistryPolicyInspectorTests()
        {
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<RegistryPolicyInspector>();
            _inspector = new RegistryPolicyInspector(logger);

            // создаем тестовый ключ в HKCU
            using var key = Registry.CurrentUser.CreateSubKey(_testKey);
        }

        [Fact]
        public void Inspect_Should_Report_NotApplied_When_Key_Missing()
        {
            var policy = new PolicyDefinition
            {
                Name = "MissingKeyPolicy",
                RegistryKey = @"Software\NoSuchKey",
                ValueName = "Value1",
                Elements = new List<PolicyElementDefinition>() // пустой список
            };

            var report = _inspector.Inspect(policy, true, "1");

            Assert.Equal(PolicyComplianceState.NotApplied, report.State);
            Assert.Equal(@"Software\NoSuchKey", report.Key);
        }

        [Fact]
        public void Inspect_Should_Report_Applied_When_Single_Key_Matches()
        {
            using var key = Registry.CurrentUser.CreateSubKey(_testKey);
            key.SetValue("Enabled", "1");

            var policy = new PolicyDefinition
            {
                Name = "SingleKeyPolicy",
                RegistryKey = _testKey,
                ValueName = "Enabled",
                Elements = new List<PolicyElementDefinition>() // пустой список
            };

            var report = _inspector.Inspect(policy, true, "1");

            Assert.Equal(PolicyComplianceState.Applied, report.State);
            Assert.Equal("Enabled", report.ValueName);
            Assert.Equal("1", report.ActualValue);
        }

        [Fact]
        public void Inspect_Should_Handle_Element_With_Childs()
        {
            using var key = Registry.CurrentUser.CreateSubKey(_testKey);
            key.SetValue("Child1", "A");
            key.SetValue("Child2", "B");

            var policy = new PolicyDefinition
            {
                Name = "ElementWithChilds",
                Elements = new List<PolicyElementDefinition>
                {
                    new PolicyElementDefinition
                    {
                        IdName = "Elem1",
                        RegistryKey = _testKey,
                        ValueName = "ElemValue",
                        Childs = new List<PolicyElementItemDefinition>
                        {
                            new PolicyElementItemDefinition { IdName = "Child1", ValueName = "Child1", Value = "A" },
                            new PolicyElementItemDefinition { IdName = "Child2", ValueName = "Child2", Value = "B" },
                        }
                    }
                }
            };

            var report = _inspector.Inspect(policy, true, null);

            Assert.Equal(PolicyComplianceState.Applied, report.State);
            var elementReport = report.ChildReports.First();
            Assert.Equal(2, elementReport.ChildReports.Count);
            Assert.All(elementReport.ChildReports, r => Assert.Equal(PolicyComplianceState.Applied, r.State));
        }

        [Fact]
        public void Inspect_Should_Report_Drifted_When_Value_Differs()
        {
            using var key = Registry.CurrentUser.CreateSubKey(_testKey);
            key.SetValue("DriftValue", "X");

            var policy = new PolicyDefinition
            {
                Name = "DriftPolicy",
                RegistryKey = _testKey,
                ValueName = "DriftValue",
                Elements = new List<PolicyElementDefinition>()
            };

            var report = _inspector.Inspect(policy, true, "Y");

            Assert.Equal(PolicyComplianceState.Drifted, report.ChildReports.First().State);
            Assert.Equal("X", report.ChildReports.First().ActualValue);
            Assert.Equal("Y", report.ChildReports.First().ExpectedValue);
        }

        [Fact]
        public void Inspect_Should_Handle_Multiple_Childs_In_ElementItem()
        {
            using var key = Registry.CurrentUser.CreateSubKey(_testKey);
            key.SetValue("SvcA", "Enabled");
            key.SetValue("SvcB", "Disabled");

            var policy = new PolicyDefinition
            {
                Name = "ListElementPolicy",
                Elements = new List<PolicyElementDefinition>
                {
                    new PolicyElementDefinition
                    {
                        IdName = "Services",
                        Childs = new List<PolicyElementItemDefinition>
                        {
                            new PolicyElementItemDefinition { IdName = "SvcA", ValueName = "SvcA", Value = "Enabled" },
                            new PolicyElementItemDefinition { IdName = "SvcB", ValueName = "SvcB", Value = "Disabled" }
                        }
                    }
                }
            };

            var report = _inspector.Inspect(policy, true, null);

            var elementReport = report.ChildReports.First();
            Assert.Equal(2, elementReport.ChildReports.Count);
            Assert.Contains(elementReport.ChildReports, r => r.ValueName == "SvcA" && (string)r.ActualValue! == "Enabled");
            Assert.Contains(elementReport.ChildReports, r => r.ValueName == "SvcB" && (string)r.ActualValue! == "Disabled");
        }
    }
}
