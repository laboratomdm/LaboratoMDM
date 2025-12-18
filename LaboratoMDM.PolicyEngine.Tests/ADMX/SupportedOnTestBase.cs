using LaboratoMDM.PolicyEngine.Implementations;
using LaboratoMDM.PolicyEngine.Tests.Support;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public abstract class SupportedOnTestBase
    {
        protected readonly InMemorySupportedOnCatalog Catalog;
        protected readonly SupportedOnResolver Resolver;

        protected SupportedOnTestBase()
        {
            Catalog = new InMemorySupportedOnCatalog();
            Resolver = new SupportedOnResolver(Catalog);
        }
    }
}
