using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public sealed class SupportedOnSimpleTests : SupportedOnTestBase
    {
        public SupportedOnSimpleTests()
        {
            Catalog.Add(new SupportedOnDefinition
            {
                Name = "SUPPORTED_Windows10",
                RootExpression = new SupportedOnReference
                {
                    Ref = "Windows10"
                }
            });
        }

        [Fact]
        public void Windows10_On_Windows11_Should_Be_Applicable()
        {
            var ctx = new PolicyEvaluationContext
            {
                OsVersion = new Version(10, 0)
            };

            var r = Resolver.Resolve("SUPPORTED_Windows10", ctx);

            Assert.Equal(PolicyApplicabilityStatus.Applicable, r.Status);
        }

        [Fact]
        public void Windows10_On_Windows7_Should_Be_NotApplicable()
        {
            var ctx = new PolicyEvaluationContext
            {
                OsVersion = new Version(6, 1)
            };

            var r = Resolver.Resolve("SUPPORTED_Windows10", ctx);

            Assert.Equal(PolicyApplicabilityStatus.NotApplicable, r.Status);
        }
    }
}
