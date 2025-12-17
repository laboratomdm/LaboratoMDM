using LaboratoMDM.Core.Models.User;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

namespace LaboratoMDM.NodeEngine.Implementations
{
    [SupportedOSPlatform("windows")]
    public sealed class UserCollectorService(ILogger<UserCollectorService> logger) : IUserCollector
    {
        private readonly ILogger<UserCollectorService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public IReadOnlyList<UserInfo> GetAllUsers()
        {
            var result = new List<UserInfo>();

            try
            {
                _logger.LogInformation("Collecting local users via PrincipalContext...");

                using var ctx = new PrincipalContext(ContextType.Machine);
                using var searcher = new PrincipalSearcher(new UserPrincipal(ctx));

                foreach (UserPrincipal user in searcher.FindAll().Cast<UserPrincipal>())
                {
                    try
                    {
                        if (user.SamAccountName == null)
                            continue;

                        var info = new UserInfo
                        {
                            Name = user.SamAccountName,
                            Sid = user.Sid?.Value,
                            AccountType = UserAccountType.Local,
                            IsEnabled = user.Enabled ?? true,
                            Description = user.Description,
                            HomeDirectory = user.HomeDirectory,
                            Groups = GetLocalUserGroups(user)
                        };

                        result.Add(info);
                        _logger.LogInformation("Found local user: {Name}", info.Name);
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogWarning(innerEx, "Failed to process user {User}", user.SamAccountName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect local users.");
            }

            return result;
        }

        private IReadOnlyList<string> GetLocalUserGroups(UserPrincipal user)
        {
            var groups = new List<string>();
            try
            {
                foreach (var g in user.GetGroups())
                {
                    groups.Add(g.SamAccountName ?? g.Name ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get local groups for user {User}", user.SamAccountName);
            }

            return groups;
        }
    }
}
