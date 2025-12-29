using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public PolicyCategoryNodeViewModel(string id, string displayName, string? explain)
    {
        Id = id;
        DisplayName = displayName;
        ExplainText = explain;
    }
}

