using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    [SupportedOSPlatform("windows")]
    [Trait("Category", "Registry")]
    public sealed class RegistryPolicyApplierTests
    {
        private const string TestRootKey = @"Software\TestPolicies";

        private readonly RegistryPolicyApplier _applier =
            new(new NullLogger<RegistryPolicyApplier>());

        private static void CleanupKey(RegistryKey root)
        {
            root.DeleteSubKeyTree(TestRootKey, false);
        }

        private static PolicyApplicationPlan CreateSimplePlan(
            PolicyScope scope,
            string valueName = "Enabled",
            object? value = null,
            bool delete = false)
        {
            var plan = new PolicyApplicationPlan
            {
                PolicyName = "TestPolicy"
            };

            plan.Operations.Add(new RegistryOperation
            {
                Scope = scope,
                Key = TestRootKey,
                ValueName = valueName,
                Value = value,
                ValueKind = RegistryValueKind.DWord,
                Delete = delete,
                Reason = "test"
            });

            return plan;
        }

        [Fact]
        public void Apply_Should_Set_Registry_Value_For_Current_User()
        {
            CleanupKey(Registry.CurrentUser);

            var plan = CreateSimplePlan(
                PolicyScope.User,
                value: 1);

            var context = new PolicyExecutionContext
            {
                IsMachine = false
            };

            _applier.Apply(plan, context);

            using var key = Registry.CurrentUser.OpenSubKey(TestRootKey);
            Assert.NotNull(key);
            Assert.Equal(1, key!.GetValue("Enabled"));
        }

        [Fact]
        public void Apply_Should_Delete_Registry_Key_For_Current_User()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(TestRootKey))
            {
                key!.SetValue("Enabled", 1, RegistryValueKind.DWord);
            }

            var plan = CreateSimplePlan(
                PolicyScope.User,
                delete: true);

            var context = new PolicyExecutionContext();

            _applier.Apply(plan, context);

            using var deletedKey = Registry.CurrentUser.OpenSubKey(TestRootKey);
            Assert.Null(deletedKey);
        }

        [Fact]
        public void Apply_Should_Handle_Multiple_Operations()
        {
            CleanupKey(Registry.CurrentUser);

            var plan = new PolicyApplicationPlan
            {
                PolicyName = "MultiPolicy"
            };

            plan.Operations.Add(new RegistryOperation
            {
                Scope = PolicyScope.User,
                Key = TestRootKey + "\\Item1",
                ValueName = "Enabled",
                Value = 1,
                ValueKind = RegistryValueKind.DWord,
                Reason = "item1"
            });

            plan.Operations.Add(new RegistryOperation
            {
                Scope = PolicyScope.User,
                Key = TestRootKey + "\\Item2",
                ValueName = "Enabled",
                Value = 2,
                ValueKind = RegistryValueKind.DWord,
                Reason = "item2"
            });

            var context = new PolicyExecutionContext();

            _applier.Apply(plan, context);

            using var key1 = Registry.CurrentUser.OpenSubKey(TestRootKey + "\\Item1");
            using var key2 = Registry.CurrentUser.OpenSubKey(TestRootKey + "\\Item2");

            Assert.NotNull(key1);
            Assert.NotNull(key2);
            Assert.Equal(1, key1!.GetValue("Enabled"));
            Assert.Equal(2, key2!.GetValue("Enabled"));
        }

        [Fact]
        public void Apply_Should_Set_Registry_Value_For_Specific_User_Sid()
        {
            var sid = WindowsIdentity.GetCurrent().User!.Value;

            using var userRoot = Registry.Users.OpenSubKey(sid, writable: true);
            Assert.NotNull(userRoot);

            userRoot!.DeleteSubKeyTree(TestRootKey, false);

            var plan = CreateSimplePlan(
                PolicyScope.User,
                value: 1);

            var context = new PolicyExecutionContext
            {
                UserSid = sid
            };

            _applier.Apply(plan, context);

            using var key = Registry.Users.OpenSubKey($"{sid}\\{TestRootKey}");
            Assert.NotNull(key);
            Assert.Equal(1, key!.GetValue("Enabled"));
        }

        [Fact]
        public void Apply_Should_Set_Registry_Value_For_Machine()
        {
            CleanupKey(Registry.LocalMachine);

            var plan = CreateSimplePlan(
                PolicyScope.Machine,
                value: 1);

            var context = new PolicyExecutionContext
            {
                IsMachine = true
            };

            _applier.Apply(plan, context);

            using var key = Registry.LocalMachine.OpenSubKey(TestRootKey);
            Assert.NotNull(key);
            Assert.Equal(1, key!.GetValue("Enabled"));
        }

        [Fact(Skip = "Integration test: requires local group setup")]
        public void Apply_Should_Apply_Policy_To_All_Users_In_Group()
        {
            var plan = CreateSimplePlan(
                PolicyScope.User,
                value: 1);

            var context = new PolicyExecutionContext
            {
                UserGroup = "Users"
            };

            _applier.Apply(plan, context);

            // Проверка зависит от окружения:
            // - нужно проверять HKU\<SID>\Software\TestPolicies
            // - корректнее делать как integration-test
        }
    }
}
