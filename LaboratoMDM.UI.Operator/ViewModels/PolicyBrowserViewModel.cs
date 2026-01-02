using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaboratoMDM.UI.Operator.ViewModels
{
    public sealed class PolicyBrowserViewModel : ObservableObject
    {
        public ObservableCollection<PolicyCategoryNodeViewModel> RootCategories { get; } = new();

        private PolicyCategoryNodeViewModel? _selectedCategory;
        public PolicyCategoryNodeViewModel? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    UpdatePolicies();
                }
            }
        }

        public ObservableCollection<PolicyItemViewModel> VisiblePolicies { get; } = new();

        private PolicyItemViewModel? _selectedPolicy;
        public PolicyItemViewModel? SelectedPolicy
        {
            get => _selectedPolicy;
            set => SetProperty(ref _selectedPolicy, value);
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplySearch();
            }
        }

        private void UpdatePolicies()
        {
            VisiblePolicies.Clear();
            if (SelectedCategory == null) return;

            foreach (var p in SelectedCategory.Policies)
                VisiblePolicies.Add(p);
        }

        private void ApplySearch()
        {
            foreach (var root in RootCategories)
                ApplySearchRecursive(root, SearchText);
        }

        private bool ApplySearchRecursive(
            PolicyCategoryNodeViewModel node,
            string text)
        {
            bool hasVisibleChild = false;

            foreach (var child in node.Children)
            {
                if (ApplySearchRecursive(child, text))
                    hasVisibleChild = true;
            }

            bool hasPolicyMatch = node.Policies.Any(p =>
                p.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

            node.IsVisible = string.IsNullOrWhiteSpace(text)
                             || hasPolicyMatch
                             || hasVisibleChild;

            node.IsExpanded = hasVisibleChild || hasPolicyMatch;

            return node.IsVisible;
        }
    }
}
