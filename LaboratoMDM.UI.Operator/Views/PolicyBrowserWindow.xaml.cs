using LaboratoMDM.UI.Operator.ViewModels;
using System.Windows;

namespace LaboratoMDM.UI.Operator.Views
{
    /// <summary>
    /// Interaction logic for PolicyBrowserWindow.xaml
    /// </summary>
    public partial class PolicyBrowserWindow : Window
    {
        public PolicyBrowserWindow()
        {
            InitializeComponent();

            DataContext = CreateTestViewModel();
        }
        private void OnCategorySelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is PolicyBrowserViewModel vm)
            {
                vm.SelectedCategory = e.NewValue as PolicyCategoryNodeViewModel;
            }
        }

        private static PolicyBrowserViewModel CreateTestViewModel()
        {
            var vm = new PolicyBrowserViewModel();

            // ===== Root category =====
            var windows = new PolicyCategoryNodeViewModel(
                id: "Windows",
                displayName: "Windows",
                explain: "Параметры операционной системы Windows");

            // ===== Subcategories =====
            var system = new PolicyCategoryNodeViewModel(
                "System",
                "System",
                "Системные параметры Windows");

            var components = new PolicyCategoryNodeViewModel(
                "Components",
                "Windows Components",
                "Компоненты Windows");

            var defender = new PolicyCategoryNodeViewModel(
                "Defender",
                "Microsoft Defender",
                "Антивирусная защита");

            var edge = new PolicyCategoryNodeViewModel(
                "Edge",
                "Microsoft Edge",
                "Настройки браузера Edge");

            // ===== Build tree =====
            windows.Children.Add(system);
            windows.Children.Add(components);

            components.Children.Add(defender);
            components.Children.Add(edge);

            // ===== Policies =====
            system.Policies.Add(new PolicyItemViewModel(
                FakePolicy(
                    name: "DisableCMD",
                    display: "Запретить командную строку",
                    explain: "Отключает доступ пользователей к cmd.exe",
                    scope: Core.Models.Policy.PolicyScope.User)));

            system.Policies.Add(new PolicyItemViewModel(
                FakePolicy(
                    name: "DisableRegistryTools",
                    display: "Запретить редактор реестра",
                    explain: "Блокирует запуск regedit.exe",
                    scope: Core.Models.Policy.PolicyScope.User)));

            defender.Policies.Add(new PolicyItemViewModel(
                FakePolicy(
                    name: "DisableRealtimeProtection",
                    display: "Отключить защиту в реальном времени",
                    explain: "Отключает постоянный мониторинг угроз",
                    scope: Core.Models.Policy.PolicyScope.Machine)));

            defender.Policies.Add(new PolicyItemViewModel(
                FakePolicy(
                    name: "DisableCloudProtection",
                    display: "Отключить облачную защиту",
                    explain: "Запрещает отправку данных в Microsoft",
                    scope: Core.Models.Policy.PolicyScope.Machine)));

            edge.Policies.Add(new PolicyItemViewModel(
                FakePolicy(
                    name: "HomepageLocation",
                    display: "Домашняя страница",
                    explain: "Указывает URL домашней страницы",
                    scope: Core.Models.Policy.PolicyScope.User)));

            edge.Policies.Add(new PolicyItemViewModel(
                FakePolicy(
                    name: "DisablePasswordManager",
                    display: "Отключить менеджер паролей",
                    explain: "Запрещает сохранение паролей",
                    scope: Core.Models.Policy.PolicyScope.User)));

            // ===== Add root =====
            vm.RootCategories.Add(windows);

            // ===== Select defaults =====
            windows.IsExpanded = true;
            components.IsExpanded = true;
            defender.IsExpanded = true;

            vm.SelectedCategory = system;

            return vm;
        }

        private static Core.Models.Policy.PolicyDefinition FakePolicy(
            string name,
            string display,
            string explain,
            Core.Models.Policy.PolicyScope scope)
        {
            return new Core.Models.Policy.PolicyDefinition
            {
                Name = name,
                DisplayName = display,
                ExplainText = explain,
                Scope = scope,
                RegistryKey = @"HKLM\Software\Policies\Test",
                ValueName = name
            };
        }
    }
}
