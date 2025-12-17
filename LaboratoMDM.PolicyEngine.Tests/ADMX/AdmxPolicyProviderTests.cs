using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Implementations;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public sealed class AdmxPolicyProviderTests : IDisposable
    {
        private readonly string _tempDir;

        public AdmxPolicyProviderTests()
        {
            _tempDir = Directory.CreateTempSubdirectory().FullName;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch {}
        }

        [Fact]
        public void LoadPolicies_Should_Load_Policies_From_Admx_File()
        {
            // Arrange
            WriteAdmx("test.admx", SimpleAdmx);

            var provider = new AdmxPolicyProvider(_tempDir);

            // Act
            var policies = provider.LoadPolicies();

            // Assert
            Assert.Single(policies);

            var policy = policies[0];
            Assert.Equal("TestPolicy", policy.Name);
            Assert.Equal(PolicyScope.Machine, policy.Scope);
            Assert.Equal(@"Software\Test", policy.RegistryKey);
            Assert.Equal("Enabled", policy.ValueName);
        }

        [Fact]
        public void FindPolicy_Should_Return_Policy_By_Name()
        {
            // Arrange
            WriteAdmx("test.admx", SimpleAdmx);
            var provider = new AdmxPolicyProvider(_tempDir);

            // Act
            var policy = provider.FindPolicy("TestPolicy");

            // Assert
            Assert.NotNull(policy);
            Assert.Equal(@"Software\Test", policy!.RegistryKey);
        }

        [Fact]
        public void FindPolicy_Should_Return_Null_When_Policy_Not_Found()
        {
            // Arrange
            WriteAdmx("test.admx", SimpleAdmx);
            var provider = new AdmxPolicyProvider(_tempDir);

            // Act
            var policy = provider.FindPolicy("UnknownPolicy");

            // Assert
            Assert.Null(policy);
        }

        [Fact]
        public void LoadPolicies_Should_Extract_SupportedOn_Reference()
        {
            // Arrange
            WriteAdmx("test.admx", AdmxWithSupportedOn);
            var provider = new AdmxPolicyProvider(_tempDir);

            // Act
            var policy = provider.FindPolicy("TestPolicy");

            // Assert
            Assert.NotNull(policy);
            Assert.Equal("SUPPORTED_WIN10", policy!.SupportedOnRef);
        }

        private void WriteAdmx(string fileName, string content)
        {
            File.WriteAllText(Path.Combine(_tempDir, fileName), content);
        }

        #region Test ADMX Samples

        private const string SimpleAdmx = """
        <?xml version="1.0" encoding="utf-8"?>
        <policyDefinitions revision="1.0"
            xmlns="http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions">

          <policies>
            <policy name="TestPolicy"
                    class="Machine"
                    key="Software\Test"
                    valueName="Enabled" />
          </policies>

        </policyDefinitions>
        """;

        private const string AdmxWithSupportedOn = """
        <?xml version="1.0" encoding="utf-8"?>
        <policyDefinitions revision="1.0"
            xmlns="http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions">

          <policies>
            <policy name="TestPolicy"
                    class="Machine"
                    key="Software\Test"
                    valueName="Enabled">
              <supportedOn ref="SUPPORTED_WIN10" />
            </policy>
          </policies>

        </policyDefinitions>
        """;

        #endregion
    }
}