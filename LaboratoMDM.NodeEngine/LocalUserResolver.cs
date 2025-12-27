using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;


namespace LaboratoMDM.NodeEngine
{
    [SupportedOSPlatform("windows")]
    public static class LocalUserResolver
    {
        public static IReadOnlyList<string> GetUserSidsByGroup(string groupName)
        {
            var result = new List<string>();

            using var ctx = new PrincipalContext(ContextType.Machine);
            using var group = GroupPrincipal.FindByIdentity(ctx, groupName);

            if (group == null)
                return result;

            foreach (var member in group.GetMembers())
            {
                if (member.Sid != null)
                    result.Add(member.Sid.Value);
            }

            return result;
        }
    }
}
