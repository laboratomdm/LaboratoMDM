using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.PolicyEngine.Services
{
    public interface IPolicyQueryService
    {
        Task<PolicyEntity?> GetById(int id);
        Task<PolicyEntity?> GetByHash(string hash);
        Task<IReadOnlyList<PolicyEntity>> GetByCategory(int categoryId);
        Task<IReadOnlyList<PolicyEntity>> FindApplicable(
            PolicyEvaluationContext context);
    }

}
