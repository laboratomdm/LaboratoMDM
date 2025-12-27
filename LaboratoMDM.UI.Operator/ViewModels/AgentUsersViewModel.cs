using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;

namespace LaboratoMDM.UI.Operator.ViewModels;

public sealed class AgentUsersViewModel : INotifyPropertyChanged
{
    private readonly UserService.UserServiceClient _userClient;
    private readonly string _agentId;

    public AgentUsersViewModel(
        string agentId,
        UserService.UserServiceClient userClient)
    {
        _agentId = agentId;
        _userClient = userClient;

        Users = new ObservableCollection<UserViewModel>();
    }

    public ObservableCollection<UserViewModel> Users { get; }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    private string? _error;
    public string? Error
    {
        get => _error;
        private set
        {
            _error = value;
            OnPropertyChanged();
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            Error = null;

            var response = await _userClient.ListUsersForAgentAsync(
                new ListUsersForAgentRequest
                {
                    AgentId = _agentId
                });

            Users.Clear();
            foreach (var user in response.Users)
            {
                Users.Add(new UserViewModel(user));
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
