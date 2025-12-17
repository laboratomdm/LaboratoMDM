using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Implementations;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public class SupportedOnResolverTests
    {
        private readonly SupportedOnResolver _resolver = new();

        [Fact]
        public void Should_Return_Applicable_For_Windows10_On_Windows11()
        {
            var context = new PolicyEvaluationContext
            {
                OsVersion = new Version(10, 0)
            };

            var result = _resolver.Resolve("SUPPORTED_Windows10", context);

            Assert.Equal(PolicyApplicabilityStatus.Applicable, result.Status);
        }

        [Fact]
        public void Should_Return_NotApplicable_For_Windows10_On_Windows7()
        {
            var context = new PolicyEvaluationContext
            {
                OsVersion = new Version(6, 1)
            };

            var result = _resolver.Resolve("SUPPORTED_Windows10", context);

            Assert.Equal(PolicyApplicabilityStatus.NotApplicable, result.Status);
        }

        [Fact]
        public void Should_Return_Unknown_For_Complex_Expression()
        {
            var context = new PolicyEvaluationContext
            {
                OsVersion = new Version(10, 0)
            };

            var result = _resolver.Resolve("SUPPORTED_WindowsXP", context);

            Assert.Equal(PolicyApplicabilityStatus.Unknown, result.Status);
        }
    }
}
