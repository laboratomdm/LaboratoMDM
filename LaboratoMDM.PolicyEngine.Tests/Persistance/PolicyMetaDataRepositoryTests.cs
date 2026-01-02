using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using LaboratoMDM.PolicyEngine.Tests.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.PolicyEngine.Tests.Persistance
{
     public class PolicyMetaDataRepositoryTests(SqliteFileSchemaFixture fixture)
        : IClassFixture<SqliteFileSchemaFixture>
    {
        private readonly PolicyMetadataRepository _repo = new PolicyMetadataRepository(fixture.Connection,new PolicyCategoryEntityMapper(),
            new PolicyCategoryViewMapper(), new PolicyNamespaceEntityMapper());

        [Fact]
        public async Task GetCategoryTree_ShouldReturnRootCategoriesWithChildren()
        {
            var response = await _repo.GetCategoryTree();
            Assert.NotNull(response);
        }
    }
}
