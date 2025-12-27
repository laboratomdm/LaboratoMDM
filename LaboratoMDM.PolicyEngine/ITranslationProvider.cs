using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.PolicyEngine;

public interface ITranslationProvider
{
    IReadOnlyList<Translation> LoadTranslations();
    Translation? FindPolicy(string id, string lang);
}