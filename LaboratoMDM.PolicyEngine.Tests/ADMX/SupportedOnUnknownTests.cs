using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public sealed class SupportedOnUnknownTests : SupportedOnTestBase
    {
        public SupportedOnUnknownTests()
        {
            Catalog.Add(new SupportedOnDefinition
            {
                Name = "SUPPORTED_Weird",
                RootExpression = new SupportedOnReference
                {
                    Ref = "WindowsXP_SP3_Embedded"
                }
            });
        }

        [Fact]
        public void Should_Return_Unknown_When_Reference_Unresolved()
        {
            var ctx = new PolicyEvaluationContext
            {
                OsVersion = new Version(10, 0)
            };

            var r = Resolver.Resolve("SUPPORTED_Weird", ctx);

            Assert.Equal(PolicyApplicabilityStatus.Unknown, r.Status);
        }
    }
}