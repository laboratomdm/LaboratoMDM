using CommunityToolkit.Mvvm.ComponentModel;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;

namespace LaboratoMDM.UI.Operator.ViewModels;
public sealed class PolicyItemViewModel : ObservableObject
{
    public PolicySummary Definition { get; }

    public long Id => Definition.Id;
    public string Name => Definition.DisplayName;
    public string? ExplainText => Definition.DisplayName;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public PolicyItemViewModel(PolicySummary definition)
    {
        Definition = definition;
    }
}

