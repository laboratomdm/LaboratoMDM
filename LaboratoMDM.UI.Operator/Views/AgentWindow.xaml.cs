using CommunityToolkit.Mvvm.Input;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.UI.Operator.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LaboratoMDM.UI.Operator.Views
{
    /// <summary>
    /// Interaction logic for AgentWindow.xaml
    /// </summary>
    public partial class AgentWindow : Window
    {
        private readonly AgentsMasterDetailViewModel _viewModel;
        private readonly PolicyCatalogService.PolicyCatalogServiceClient _policyCatalogServiceClient;
        private PolicyBrowserWindow _policyBrowserWindow;
        public ICommand ApplyPolicyCommand { get; }


        public AgentWindow(
            AgentService.AgentServiceClient agentClient,
            UserService.UserServiceClient userClient,
            PolicyCatalogService.PolicyCatalogServiceClient policyCatalogServiceClient)
        {
            InitializeComponent();

            ApplyPolicyCommand = new RelayCommand<object>(OnApplyPolicy);

            _policyCatalogServiceClient = policyCatalogServiceClient;

            // Создаем ViewModel с gRPC клиентом
            _viewModel = new AgentsMasterDetailViewModel(agentClient, userClient);

            // Назначаем DataContext для биндинга XAML
            DataContext = _viewModel;

            // Загружаем список агентов при старте окна
            Loaded += async (_, __) => await _viewModel.RefreshCommand.ExecuteAsync(null);

            _viewModel.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(AgentsMasterDetailViewModel.SelectedAgent)
                    && _viewModel.SelectedAgent != null)
                {
                    await _viewModel.LoadSelectedAgentDetailsAsync(_viewModel.SelectedAgent.AgentId);
                    await _viewModel.SelectedAgentDetail.Users.LoadAsync();
                }
            };
        }

        private void RibbonButton_Click(object sender, RoutedEventArgs e)
        {
            var policyEditorWindow = new PolicyEditorWindow(@"C:\PolicyDefinitions\ru-RU\RemovableStorage.adml");
            policyEditorWindow.Show();
        }

        private void PolicyCategories_Click(object sender, RoutedEventArgs e)
        {
            if (_policyBrowserWindow != null && !_policyBrowserWindow.IsActive)
            {
                _policyBrowserWindow.Show();
            }
        }

        // Агент
        private void AgentContext_ShowAppliedPolicies(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedAgent == null)
                return;

            OpenPolicyBrowserWindow(_viewModel.SelectedAgent);
        }

        private void AgentContext_ApplyPolicy(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedAgent == null)
                return;

            OpenPolicyBrowserWindow(_viewModel.SelectedAgent);
        }

        // Пользователь
        //public ICommand ApplyPolicyCommand => new RelayCommand<object>(OnApplyPolicy);

        private void OnApplyPolicy(object? target)
        {
            if (target == null)
                return;

            OpenPolicyBrowserWindow(target);
        }


        private void OpenPolicyBrowserWindow(object target)
        {
            // Если окно еще не создан — создаем
            if (_policyBrowserWindow == null)
            {
                _policyBrowserWindow = new PolicyBrowserWindow(_policyCatalogServiceClient);
            }

            // Передаем выбранный агент или пользователя в ViewModel окна
            if (_policyBrowserWindow.DataContext is PolicyBrowserViewModel vm)
            {
                vm.SelectedTarget = target; // добавляем это свойство в PolicyBrowserViewModel
            }

            // Показываем окно
            if (!_policyBrowserWindow.IsVisible)
            {
                _policyBrowserWindow.Show();
            }
            else
            {
                _policyBrowserWindow.Activate();
            }
        }

    }
}
