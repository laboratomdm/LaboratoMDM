using System.Runtime.Versioning;
using System.Security.Principal;

namespace LaboratoMDM.NodeEngine.Tests.Attributes
{
    [SupportedOSPlatform("windows")]
    public class AdminRequiredFactAttribute : FactAttribute
    {
        public AdminRequiredFactAttribute()
        {
            if (!IsAdministrator())
            {
                Skip = "Test required run by Administrator.";
            }
        }

        private static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

}
