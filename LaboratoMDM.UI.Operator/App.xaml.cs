using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.Client;
using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;
using LaboratoMDM.UI.Operator.Views;

namespace LaboratoMDM.UI.Operator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

        public App()
        {
            //var services = new ServiceCollection();

            //// Регистрируем gRPC клиента
            //services.AddSingleton(sp =>
            //    new AgentService.AgentServiceClient(
            //        GrpcChannel.ForAddress("http://localhost:5000")));

            //services.AddSingleton(sp =>
            //    new UserService.UserServiceClient(
            //        GrpcChannel.ForAddress("http://localhost:5000")));

            //// Регистрируем окно через DI
            //services.AddTransient<AgentWindow>();

            //Services = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Открываем главное окно (или агентское окно)
            //var window = Services.GetRequiredService<AgentWindow>();
            //window.Show();
            var policyEditorWindow = new PolicyEditorWindow(@"C:\PolicyDefinitions\ru-RU\RemovableStorage.adml");
            policyEditorWindow.Show();
        }
    }
}
