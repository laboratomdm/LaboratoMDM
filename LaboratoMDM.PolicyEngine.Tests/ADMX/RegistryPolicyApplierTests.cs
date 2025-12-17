using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Implementations;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    [SupportedOSPlatform("windows")]
    [Trait("Category", "Registry")]
    public class RegistryPolicyApplierTests
    {
        private const string TestRootKey = @"Software\TestPolicies";

        private readonly RegistryPolicyApplier _applier = new();

        private void CleanupKey(PolicyScope scope)
        {
            var root = scope == PolicyScope.Machine ? Registry.LocalMachine : Registry.CurrentUser;
            root.DeleteSubKeyTree(TestRootKey, false);
        }

        [Fact]
        public void Apply_Should_Set_Registry_Value_When_Enabled()
        {
            CleanupKey(PolicyScope.User);

            var plan = new PolicyApplicationPlan
            {
                PolicyName = "TestPolicy",
                Operations =
                {
                    new RegistryOperation
                    {
                        Scope = PolicyScope.User,
                        Key = TestRootKey,
                        ValueName = "Enabled",
                        Value = 1,
                        ValueKind = RegistryValueKind.DWord
                    }
                }
            };

            _applier.Apply(plan);

            using var key = Registry.CurrentUser.OpenSubKey(TestRootKey);
            Assert.NotNull(key);
            Assert.Equal(1, key.GetValue("Enabled"));
        }

        [Fact]
        public void Apply_Should_Delete_Key_When_Delete_Operation()
        {
            var root = Registry.CurrentUser;
            using (var key = root.CreateSubKey(TestRootKey))
            {
                key.SetValue("Enabled", 1, RegistryValueKind.DWord);
            }

            var plan = new PolicyApplicationPlan
            {
                PolicyName = "TestPolicy",
                Operations =
                {
                    new RegistryOperation
                    {
                        Scope = PolicyScope.User,
                        Key = TestRootKey,
                        Delete = true
                    }
                }
            };

            _applier.Apply(plan);

            using var deletedKey = root.OpenSubKey(TestRootKey);
            Assert.Null(deletedKey);
        }

        [Fact]
        public void Apply_Should_Handle_Multiple_Operations()
        {
            CleanupKey(PolicyScope.User);

            var plan = new PolicyApplicationPlan
            {
                PolicyName = "ListPolicy",
                Operations =
                {
                    new RegistryOperation
                    {
                        Scope = PolicyScope.User,
                        Key = TestRootKey + "\\Item1",
                        ValueName = "Enabled",
                        Value = 1,
                        ValueKind = RegistryValueKind.DWord
                    },
                    new RegistryOperation
                    {
                        Scope = PolicyScope.User,
                        Key = TestRootKey + "\\Item2",
                        ValueName = "Enabled",
                        Value = 2,
                        ValueKind = RegistryValueKind.DWord
                    }
                }
            };

            _applier.Apply(plan);

            using var key1 = Registry.CurrentUser.OpenSubKey(TestRootKey + "\\Item1");
            using var key2 = Registry.CurrentUser.OpenSubKey(TestRootKey + "\\Item2");

            Assert.NotNull(key1);
            Assert.NotNull(key2);
            Assert.Equal(1, key1.GetValue("Enabled"));
            Assert.Equal(2, key2.GetValue("Enabled"));
        }
    }
}
