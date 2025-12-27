using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using System.Collections.ObjectModel;

namespace LaboratoMDM.UI.Operator.ViewModels;

public class AgentsMasterDetailViewModel : ObservableObject
{
    private readonly AgentService.AgentServiceClient _agentClient;
    private readonly UserService.UserServiceClient _userClient;

    public AgentsMasterDetailViewModel(
        AgentService.AgentServiceClient agentClient, 
        UserService.UserServiceClient userClient)
    {
        _agentClient = agentClient;
        _userClient = userClient;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<AgentSummaryViewModel> Agents { get; } = new();

    private AgentSummaryViewModel _selectedAgent;
    public AgentSummaryViewModel SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            _selectedAgent = value;
            OnPropertyChanged();
        }
    }

    private AgentDetailViewModel _selectedAgentDetail;
    public AgentDetailViewModel SelectedAgentDetail
    {
        get => _selectedAgentDetail;
        set { _selectedAgentDetail = value; OnPropertyChanged(); }
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    private async Task RefreshAsync()
    {
        Agents.Clear();
        var response = await _agentClient.ListAgentsAsync(new ListAgentsRequest());
        foreach (var a in response.Agents.OrderByDescending(x => x.IsOnline))
        {
            Agents.Add(new AgentSummaryViewModel(a));
        }
    }

    public async Task LoadSelectedAgentDetailsAsync(string agentId)
    {
        var detail = await _agentClient.GetAgentAsync(new GetAgentRequest { AgentId = agentId });
        SelectedAgentDetail = new AgentDetailViewModel(detail, _userClient);
    }
}

// ViewModel для строки в списке агентов
public class AgentSummaryViewModel(AgentSummary summary) : ObservableObject
{
    public string AgentId { get; } = summary.AgentId;
    public string HostName { get; } = summary.HostName;
    public string IpAddress { get; } = summary.IpAddress;
    public bool IsOnline { get; } = summary.IsOnline;
    public DateTime LastHeartbeat { get; } = 
        DateTimeOffset.FromUnixTimeSeconds(summary.LastHeartbeatUnix).DateTime;
}

// ViewModel для Detail
public class AgentDetailViewModel : ObservableObject
{
    public AgentDetailViewModel(AgentDetails details, UserService.UserServiceClient userClient)
    {
        AgentId = details.AgentId;
        HostName = details.HostName;
        IpAddress = details.IpAddress;
        IsOnline = details.IsOnline;
        LastHeartbeat = DateTimeOffset.FromUnixTimeSeconds(details.LastHeartbeatUnix).DateTime;

        if (details.NodeInfo != null)
        {
            var sys = details.NodeInfo.SystemInfo;
            Cpu = sys.Cpu;
            RamGb = sys.RamGb;
            OsVersion = sys.OsVersion;
            HostnameFull = sys.HostName;
            Disks = sys.Disks.ToList();
            Gpus = sys.Gpu.ToList();
            IpAddresses = sys.IpAddresses.ToList();
            MacAddresses = sys.MacAddresses.ToList();
            Motherboard = sys.Motherboard;

            LastBootTime = DateTimeOffset.FromUnixTimeSeconds(details.NodeInfo.LastBootTimeUnix).DateTime;
            OsBuild = details.NodeInfo.OsBuild;
            IsDomainJoined = details.NodeInfo.IsDomainJoined;
            AntivirusStatus = details.NodeInfo.AntivirusStatus;
            TimeZone = details.NodeInfo.TimeZone;
            Manufacturer = details.NodeInfo.Manufacturer;
            Model = details.NodeInfo.Model;
            FirmwareVersion = details.NodeInfo.FirmwareVersion;

            Users = new AgentUsersViewModel(AgentId, userClient);
        }
    }

    public string AgentId { get; }
    public string HostName { get; }
    public string IpAddress { get; }
    public bool IsOnline { get; }
    public DateTime LastHeartbeat { get; }

    public string Cpu { get; }
    public int RamGb { get; }
    public string OsVersion { get; }
    public string HostnameFull { get; }
    public List<string> Disks { get; }
    public List<string> Gpus { get; }
    public List<string> IpAddresses { get; }
    public List<string> MacAddresses { get; }
    public string Motherboard { get; }

    public DateTime LastBootTime { get; }
    public string OsBuild { get; }
    public bool IsDomainJoined { get; }
    public string AntivirusStatus { get; }
    public string TimeZone { get; }
    public string Manufacturer { get; }
    public string Model { get; }
    public string FirmwareVersion { get; }

    public AgentUsersViewModel Users { get; }
}
