using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LaboratoMDM.UI.Operator.Models;

public class AgentSummaryModel : INotifyPropertyChanged
{
    private bool _isOnline;

    public string AgentId { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsOnline
    {
        get => _isOnline;
        set { _isOnline = value; OnPropertyChanged(); }
    }
    public DateTime LastHeartbeat { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
