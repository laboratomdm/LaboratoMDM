using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.UI.Operator.ViewModels;

namespace LaboratoMDM.UI.Operator.Utils
{
    public static class PolicyTreeBuilder
    {
        public static IReadOnlyCollection<PolicyCategoryNodeViewModel> Build(
            IEnumerable<PolicyCategoryDefinition> categories,
            IEnumerable<PolicyDefinition> policies)
        {
            var dict = categories.ToDictionary(
                c => c.Name,
                c => new PolicyCategoryNodeViewModel(
                    c.Name,
                    c.DisplayName,
                    c.ExplainText));

            // Строим иерархию
            foreach (var cat in categories)
            {
                if (cat.ParentCategoryRef != null &&
                    dict.TryGetValue(cat.ParentCategoryRef.Split(':').Last(), out var parent))
                {
                    parent.Children.Add(dict[cat.Name]);
                }
            }

            // Раскладываем политики
            foreach (var policy in policies)
            {
                if (policy.ParentCategoryRef == null) continue;

                var key = policy.ParentCategoryRef.Split(':').Last();
                if (dict.TryGetValue(key, out var category))
                {
                    category.Policies.Add(new PolicyItemViewModel(policy));
                }
            }

            return dict.Values
                .Where(c => categories.First(x => x.Name == c.Id).ParentCategoryRef == null)
                .ToList();
        }
    }

}
