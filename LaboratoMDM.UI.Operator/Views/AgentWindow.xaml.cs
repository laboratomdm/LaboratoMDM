using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.UI.Operator.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LaboratoMDM.UI.Operator.Views
{
    /// <summary>
    /// Interaction logic for AgentWindow.xaml
    /// </summary>
    public partial class AgentWindow : Window
    {
        private readonly AgentsMasterDetailViewModel _viewModel;
        private readonly PolicyBrowserWindow _policyBrowserWindow;

        public AgentWindow(
            AgentService.AgentServiceClient agentClient,
            UserService.UserServiceClient userClient,
            PolicyBrowserWindow policyBrowserWindow)
        {
            InitializeComponent();

            // Создаем ViewModel с gRPC клиентом
            _viewModel = new AgentsMasterDetailViewModel(agentClient, userClient);
            _policyBrowserWindow = policyBrowserWindow;

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
    }
}
