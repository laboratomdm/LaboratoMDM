using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Implementations;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public class AdmxApplicabilityCheckerTests
    {
        [Fact]
        public void Policy_Without_SupportedOn_Should_Be_Applicable()
        {
            var checker = new AdmxApplicabilityChecker(
                new FakeSupportedOnResolver());

            var policy = new PolicyDefinition
            {
                Name = "TestPolicy",
                SupportedOnRef = null
            };

            var result = checker.Check(policy, new PolicyEvaluationContext());

            Assert.Equal(PolicyApplicabilityStatus.Applicable, result.Status);
        }

        [Fact]
        public void Policy_With_SupportedOn_Should_Invoke_Resolver()
        {
            var checker = new AdmxApplicabilityChecker(
                new FakeSupportedOnResolver());

            var policy = new PolicyDefinition
            {
                Name = "TestPolicy",
                SupportedOnRef = "TEST"
            };

            var result = checker.Check(policy, new PolicyEvaluationContext());

            Assert.Equal("Resolved by fake resolver", result.Reason);
        }

        private sealed class FakeSupportedOnResolver : ISupportedOnResolver
        {
            public PolicyApplicabilityResult Resolve(string supportedOnRef, PolicyEvaluationContext context)
                => new()
                {
                    Status = PolicyApplicabilityStatus.Applicable,
                    Reason = "Resolved by fake resolver"
                };
        }
    }

}
