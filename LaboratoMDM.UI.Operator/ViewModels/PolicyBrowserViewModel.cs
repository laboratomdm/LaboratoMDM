using CommunityToolkit.Mvvm.ComponentModel;
using Grpc.Core;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using System.Collections.ObjectModel;

namespace LaboratoMDM.UI.Operator.ViewModels
{
    public sealed class PolicyBrowserViewModel : ObservableObject
    {
        public ObservableCollection<PolicyCategoryNodeViewModel> RootCategories { get; } = new();
        public PolicyEditorViewModel PolicyEditorVM { get; } = new();

        private readonly PolicyCatalogService.PolicyCatalogServiceClient _policyCatalogServiceClient;

        public PolicyBrowserViewModel(PolicyCatalogService.PolicyCatalogServiceClient policyCatalogServiceClient)
        {
            _policyCatalogServiceClient = policyCatalogServiceClient;
            GetPolicies();
        }

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
            set
            {
                if (SetProperty(ref _selectedPolicy, value))
                {
                    LoadPolicyDetails(_selectedPolicy);
                }
            }
        }

        private PolicyDetails? _policyDetailsForEditor;
        public PolicyDetails? PolicyDetailsForEditor
        {
            get => _policyDetailsForEditor;
            set => SetProperty(ref _policyDetailsForEditor, value);
        }

        private async void LoadPolicyDetails(PolicyItemViewModel? policy)
        {
            if (policy == null) return;

            try
            {
                var details = await _policyCatalogServiceClient.GetPolicyDetailsAsync(
                    new GetPolicyDetailsRequest
                    {
                        PolicyId = policy.Id,
                        LangCode = "ru-RU"
                    });

                PolicyEditorVM.LoadFromPolicyDetails(details);
            }
            catch (RpcException ex)
            {
                Console.WriteLine(ex.Message);
            }
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

        // todo change search by policies
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

        private void GetPolicies()
        {
            var groups = _policyCatalogServiceClient.ListPoliciesGroupedByScope(new ListPoliciesGroupedByScopeRequest() { LangCode = "ru-RU" }).Groups;
            foreach (var group in groups)
            {
                var category = new PolicyCategoryNodeViewModel(group.Scope, group.Scope);
                foreach (var item in group.Policies)
                {
                    category.Policies.Add(new PolicyItemViewModel(item));
                }
                RootCategories.Add(category);
            }
        }
    }
}
