using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace LaboratoMDM.UI.Operator.ViewModels;

public sealed class PolicyCategoryNodeViewModel : ObservableObject
{
    public string Id { get; }
    public string DisplayName { get; }
    public string? ExplainText { get; }

    public ObservableCollection<PolicyCategoryNodeViewModel> Children { get; } = new();

    // Политики, лежащие прямо в этой категории
    public ObservableCollection<PolicyItemViewModel> Policies { get; } = new();

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public PolicyCategoryNodeViewModel(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
}

