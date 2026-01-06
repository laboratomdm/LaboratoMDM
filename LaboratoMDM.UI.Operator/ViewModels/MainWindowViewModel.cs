using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaboratoMDM.UI.Operator.Views;
using System.Windows.Input;

namespace LaboratoMDM.UI.Operator.ViewModels
{
    public sealed class MainWindowViewModel : ObservableObject
    {
        private readonly AgentsMasterDetailViewModel _agentsVm;
        private readonly PolicyBrowserWindow _policyBrowserWindow;

        public MainWindowViewModel(
            AgentsMasterDetailViewModel agentsVm,
            PolicyBrowserWindow policyBrowserWindow)
        {
            _agentsVm = agentsVm;
            _policyBrowserWindow = policyBrowserWindow;

            // DASHBOARD
            OpenDashboardOverviewCommand = new RelayCommand(() => { /* смена View */ });
            OpenEventsCommand = new RelayCommand(() => { });
            ShowActiveAgentsCommand = new RelayCommand(() => { });
            ShowPolicyErrorsCommand = new RelayCommand(() => { });

            // NETWORK / AGENTS
            ShowAllAgentsCommand = new RelayCommand(() => CurrentViewModel = _agentsVm);
            ShowOnlineAgentsCommand = new RelayCommand(() => { });
            ShowOfflineAgentsCommand = new RelayCommand(() => { });

            RefreshAgentStatusCommand = new RelayCommand(
                () => _agentsVm.RefreshCommand.Execute(null),
                () => HasSelectedAgent);

            RebootAgentCommand = new RelayCommand(
                () => { },//_agentsVm.RebootSelectedAgent(),
                () => HasSelectedAgent);

            ShutdownAgentCommand = new RelayCommand( 
                () => { },//_agentsVm.ShutdownSelectedAgent(),
                () => HasSelectedAgent
                );

            RemoveAgentCommand = new RelayCommand(
                () => { },//_agentsVm.RemoveSelectedAgent(),
                () => HasSelectedAgent);

            ShowIpConfigCommand = new RelayCommand(
                () => { },//_agentsVm.ShowIpConfig(),
                () => HasSelectedAgent);

            PingAgentCommand = new RelayCommand(
                () => { },//_agentsVm.PingSelectedAgent(),
                () => HasSelectedAgent);

            // POLICIES
            ShowAllPoliciesCommand = new RelayCommand(() => _policyBrowserWindow.Show());
            ShowPolicyTemplatesCommand = new RelayCommand(() => { });
            ShowArchivedPoliciesCommand = new RelayCommand(() => { });

            CreatePolicyCommand = new RelayCommand(() => OpenPolicyEditor());
            EditPolicyCommand = new RelayCommand(() => { }, () => HasSelectedPolicy);
            ClonePolicyCommand = new RelayCommand(() => { }, () => HasSelectedPolicy);
            DeletePolicyCommand = new RelayCommand(() => { }, () => HasSelectedPolicy);

            // WINDOWS POLICIES
            OpenPasswordPolicyCommand = new RelayCommand(() => OpenAdmx("Passwords"));
            OpenBitLockerPolicyCommand = new RelayCommand(() => OpenAdmx("BitLocker"));
            OpenFirewallPolicyCommand = new RelayCommand(() => OpenAdmx("Firewall"));
            OpenServicesPolicyCommand = new RelayCommand(() => OpenAdmx("Services"));
            OpenRegistryPolicyCommand = new RelayCommand(() => OpenAdmx("Registry"));

            // DEVICE POLICIES
            ShowAssignedPoliciesCommand = new RelayCommand(() => { }, () => HasSelectedAgent);
            ShowPolicyHistoryCommand = new RelayCommand(() => { }, () => HasSelectedAgent);
            ReapplyPoliciesCommand = new RelayCommand(() => { }, () => HasSelectedAgent);
            RollbackPoliciesCommand = new RelayCommand(() => { }, () => HasSelectedAgent);
            SyncPoliciesCommand = new RelayCommand(() => { }, () => HasSelectedAgent);

            CurrentViewModel = _agentsVm;
        }

        // STATE
        public object CurrentViewModel { get; private set; }

        public bool HasSelectedAgent => _agentsVm.SelectedAgent != null;
        public bool HasSelectedPolicy => false;

        // COMMANDS
        public ICommand OpenDashboardOverviewCommand { get; }
        public ICommand OpenEventsCommand { get; }
        public ICommand ShowActiveAgentsCommand { get; }
        public ICommand ShowPolicyErrorsCommand { get; }

        public ICommand ShowAllAgentsCommand { get; }
        public ICommand ShowOnlineAgentsCommand { get; }
        public ICommand ShowOfflineAgentsCommand { get; }

        public ICommand RefreshAgentStatusCommand { get; }
        public ICommand RebootAgentCommand { get; }
        public ICommand ShutdownAgentCommand { get; }
        public ICommand RemoveAgentCommand { get; }
        public ICommand ShowIpConfigCommand { get; }
        public ICommand PingAgentCommand { get; }

        public ICommand ShowAllPoliciesCommand { get; }
        public ICommand ShowPolicyTemplatesCommand { get; }
        public ICommand ShowArchivedPoliciesCommand { get; }
        public ICommand CreatePolicyCommand { get; }
        public ICommand EditPolicyCommand { get; }
        public ICommand ClonePolicyCommand { get; }
        public ICommand DeletePolicyCommand { get; }

        public ICommand OpenPasswordPolicyCommand { get; }
        public ICommand OpenBitLockerPolicyCommand { get; }
        public ICommand OpenFirewallPolicyCommand { get; }
        public ICommand OpenServicesPolicyCommand { get; }
        public ICommand OpenRegistryPolicyCommand { get; }

        public ICommand ShowAssignedPoliciesCommand { get; }
        public ICommand ShowPolicyHistoryCommand { get; }
        public ICommand ReapplyPoliciesCommand { get; }
        public ICommand RollbackPoliciesCommand { get; }
        public ICommand SyncPoliciesCommand { get; }

        private void OpenPolicyEditor() { }
        private void OpenAdmx(string name) { }
    }

}
