using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public sealed class SupportedOnAndTests : SupportedOnTestBase
    {
        public SupportedOnAndTests()
        {
            Catalog.Add(new SupportedOnDefinition
            {
                Name = "SUPPORTED_Win10_21H2",
                RootExpression = new SupportedOnAnd
                {
                    Items =
                    {
                        new SupportedOnReference { Ref = "Windows10" },
                        new SupportedOnRange
                        {
                            Ref = "Windows10",
                            MinVersionIndex = 19044
                        }
                    }
                }
            });
        }

        [Fact]
        public void Should_Be_Applicable_When_All_Conditions_Match()
        {
            var ctx = new PolicyEvaluationContext
            {
                OsVersion = new Version(10, 18363)
            };

            var r = Resolver.Resolve("SUPPORTED_Win10_21H2", ctx);

            Assert.Equal(PolicyApplicabilityStatus.Applicable, r.Status);
        }

        [Fact]
        public void Should_Be_NotApplicable_When_Build_Too_Low()
        {
            var ctx = new PolicyEvaluationContext
            {
                OsVersion = new Version(10, 18363)
            };

            var r = Resolver.Resolve("SUPPORTED_Win10_21H2", ctx);

            Assert.Equal(PolicyApplicabilityStatus.NotApplicable, r.Status);
        }
    }
}
