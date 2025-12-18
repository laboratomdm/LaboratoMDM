using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public sealed class SupportedOnOrTests : SupportedOnTestBase
    {
        public SupportedOnOrTests()
        {
            Catalog.Add(new SupportedOnDefinition
            {
                Name = "SUPPORTED_Win10_Or_Win11",
                RootExpression = new SupportedOnOr
                {
                    Items =
                    {
                        new SupportedOnReference { Ref = "Windows10" },
                        new SupportedOnReference { Ref = "Windows11" }
                    }
                }
            });
        }

        [Fact]
        public void Should_Match_Windows11()
        {
            var ctx = new PolicyEvaluationContext
            {
                OsVersion = new Version(10, 0)
            };

            var r = Resolver.Resolve("SUPPORTED_Win10_Or_Win11", ctx);

            Assert.Equal(PolicyApplicabilityStatus.Applicable, r.Status);
        }
    }
}
