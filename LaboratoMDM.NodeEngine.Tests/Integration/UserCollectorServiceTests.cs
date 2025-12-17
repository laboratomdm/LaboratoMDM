using LaboratoMDM.NodeEngine.Implementations;
using LaboratoMDM.NodeEngine.Tests.Attributes;
using Microsoft.Extensions.Logging;
using Moq;
using System.DirectoryServices.AccountManagement;
using System.Reflection;
using System.Runtime.Versioning;

namespace LaboratoMDM.NodeEngine.Tests.Integration
{
    [SupportedOSPlatform("windows")]
    public class UserCollectorServiceTests
    {
        private readonly UserCollectorService _service;

        public UserCollectorServiceTests()
        {
            var loggerMock = new Mock<ILogger<UserCollectorService>>();
            _service = new UserCollectorService(loggerMock.Object);
        }

        [AdminRequiredFact]
        public void GetAllUsers_Should_Return_List_Of_Users()
        {
            var users = _service.GetAllUsers();

            Assert.NotNull(users);
            Assert.All(users, user =>
            {
                Assert.False(string.IsNullOrWhiteSpace(user.Name));
                Assert.NotNull(user.Sid);
                Assert.NotNull(user.Groups);
            });
        }

        [AdminRequiredFact]
        public void GetLocalUserGroups_Should_Contain_Current_User()
        {
            string currentUserName = Environment.UserName;

            using var ctx = new PrincipalContext(ContextType.Machine);
            var currentUser = UserPrincipal.FindByIdentity(ctx, currentUserName);
            Assert.NotNull(currentUser);

            // Вызов приватного метода через Reflection
            var method = typeof(UserCollectorService)
                .GetMethod("GetLocalUserGroups", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.NotNull(method);

            var groups = (IReadOnlyList<string>)method!.Invoke(_service, new object[] { currentUser! })!;

            Assert.NotNull(groups);
            Assert.Contains(groups, g => 
                string.Equals(g, "Администраторы", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(g, "Administrators", StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}
