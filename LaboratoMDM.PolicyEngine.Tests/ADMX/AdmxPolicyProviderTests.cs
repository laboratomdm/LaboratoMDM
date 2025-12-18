using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Implementations;
using Microsoft.Extensions.Logging.Abstractions;

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
            catch { /* ignore */ }
        }

        [Fact]
        public void LoadPolicies_Should_Load_Policies_From_Admx_File()
        {
            // Arrange
            WriteAdmx("simple.admx", SimpleAdmx);
            var provider = new AdmxPolicyProvider(_tempDir, NullLogger<AdmxPolicyProvider>.Instance);

            // Act
            var policies = provider.LoadPolicies();

            // Assert
            Assert.Single(policies);

            var policy = policies[0];
            Assert.Equal("TestPolicy", policy.Name);
            Assert.Equal(PolicyScope.Machine, policy.Scope);
            Assert.Equal(@"Software\Test", policy.RegistryKey);
            Assert.Equal("Enabled", policy.ValueName);
            Assert.Null(policy.EnabledValue);
            Assert.Null(policy.DisabledValue);
            Assert.Empty(policy.ListKeys);
            Assert.Null(policy.SupportedOnRef);
        }

        [Fact]
        public void FindPolicy_Should_Return_Policy_By_Name()
        {
            WriteAdmx("simple.admx", SimpleAdmx);
            var provider = new AdmxPolicyProvider(_tempDir, NullLogger<AdmxPolicyProvider>.Instance);

            var policy = provider.FindPolicy("TestPolicy");

            Assert.NotNull(policy);
            Assert.Equal(@"Software\Test", policy!.RegistryKey);
        }

        [Fact]
        public void FindPolicy_Should_Return_Null_When_Policy_Not_Found()
        {
            WriteAdmx("simple.admx", SimpleAdmx);
            var provider = new AdmxPolicyProvider(_tempDir, NullLogger<AdmxPolicyProvider>.Instance);

            var policy = provider.FindPolicy("UnknownPolicy");

            Assert.Null(policy);
        }

        [Fact]
        public void LoadPolicies_Should_Extract_SupportedOn_Reference()
        {
            WriteAdmx("supportedon.admx", AdmxWithSupportedOn);
            var provider = new AdmxPolicyProvider(_tempDir, NullLogger<AdmxPolicyProvider>.Instance);

            var policy = provider.FindPolicy("TestPolicy");

            Assert.NotNull(policy);
            Assert.Equal("SUPPORTED_WIN10", policy!.SupportedOnRef);
        }

        [Fact]
        public void LoadPolicies_Should_Read_EnabledDisabledValues_And_ListKeys()
        {
            WriteAdmx("full.admx", AdmxFull);
            var provider = new AdmxPolicyProvider(_tempDir, NullLogger<AdmxPolicyProvider>.Instance);

            var policy = provider.FindPolicy("FullPolicy");

            Assert.NotNull(policy);
            Assert.Equal(1, policy!.EnabledValue);
            Assert.Equal(0, policy.DisabledValue);
            Assert.Equal(2, policy.ListKeys.Count);
            Assert.Contains("Key1", policy.ListKeys);
            Assert.Contains("Key2", policy.ListKeys);
        }

        [Fact]
        public void LoadPolicies_Should_Handle_Both_And_None_Scopes()
        {
            WriteAdmx("scope.admx", AdmxScopes);
            var provider = new AdmxPolicyProvider(_tempDir, NullLogger<AdmxPolicyProvider>.Instance);

            var policyBoth = provider.FindPolicy("BothPolicy");
            var policyNone = provider.FindPolicy("NonePolicy");

            Assert.NotNull(policyBoth);
            Assert.Equal(PolicyScope.Both, policyBoth!.Scope);

            Assert.NotNull(policyNone);
            Assert.Equal(PolicyScope.None, policyNone!.Scope);
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

        private const string AdmxFull = """
        <?xml version="1.0" encoding="utf-8"?>
        <policyDefinitions revision="1.0"
            xmlns="http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions">

          <policies>
            <policy name="FullPolicy"
                    class="User"
                    key="Software\Full"
                    valueName="Enabled">
              <enabledValue>
                <decimal value="1"/>
              </enabledValue>
              <disabledValue>
                <decimal value="0"/>
              </disabledValue>
              <list key="Key1"/>
              <list key="Key2"/>
            </policy>
          </policies>

        </policyDefinitions>
        """;

        private const string AdmxScopes = """
        <?xml version="1.0" encoding="utf-8"?>
        <policyDefinitions revision="1.0"
            xmlns="http://schemas.microsoft.com/GroupPolicy/2006/07/PolicyDefinitions">

          <policies>
            <policy name="BothPolicy"
                    class="Both"
                    key="Software\Both"
                    valueName="Enabled" />
            <policy name="NonePolicy"
                    class="Unknown"
                    key="Software\None"
                    valueName="Enabled" />
          </policies>

        </policyDefinitions>
        """;

        #endregion
    }
}