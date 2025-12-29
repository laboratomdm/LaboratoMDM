using CommunityToolkit.Mvvm.ComponentModel;
using LaboratoMDM.Core.Models.Policy;

namespace LaboratoMDM.UI.Operator.ViewModels;
public sealed class PolicyItemViewModel : ObservableObject
{
    public PolicyDefinition Definition { get; }

    public string Name => Definition.DisplayName ?? Definition.Name;
    public string? ExplainText => Definition.ExplainText;
    public PolicyScope Scope => Definition.Scope;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public PolicyItemViewModel(PolicyDefinition definition)
    {
        Definition = definition;
    }
}

