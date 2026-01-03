using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.UI.Operator.ViewModels;
using System.Windows;

namespace LaboratoMDM.UI.Operator.Views
{
    /// <summary>
    /// Interaction logic for PolicyBrowserWindow.xaml
    /// </summary>
    public partial class PolicyBrowserWindow : Window
    {
        private readonly PolicyBrowserViewModel _policyBrowserViewModel;
        private readonly PolicyCatalogService.PolicyCatalogServiceClient _policyCatalogServiceClient;
        public PolicyBrowserWindow(PolicyCatalogService.PolicyCatalogServiceClient policyCatalogServiceClient)
        {
            InitializeComponent();
            _policyCatalogServiceClient = policyCatalogServiceClient;
            _policyBrowserViewModel = new PolicyBrowserViewModel(_policyCatalogServiceClient);

            DataContext = _policyBrowserViewModel;
        }

        private void OnCategorySelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is PolicyBrowserViewModel vm)
            {
                vm.SelectedCategory = e.NewValue as PolicyCategoryNodeViewModel;
            }
        }
    }
}
