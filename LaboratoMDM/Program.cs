// See https://aka.ms/new-console-template for more information
using LaboratoMDM.ActiveDirectory.Service;
using LaboratoMDM.ActiveDirectory.Service.Implementations;
using LaboratoMDM.ActiveDirectory.Service.Rsop;
using LaboratoMDM.Core.Models;
using LaboratoMDM.NodeEngine;
using LaboratoMDM.NodeEngine.Implementations;
using LaboratoMDM.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
class Program
{
    static void Main(string[] args)
    {
        // Настройка DI и логгера
        var serviceProvider = ConfigureServices();

        // Выполняем сбор и вывод информации
        Run(serviceProvider);

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Information);
        });

        // Добавляем коллекторы через DI
        services.AddSingleton<IAdCollector, AdCollector>();
        services.AddSingleton<INodeSystemInfoCollector, NodeSystemInfoCollector>();
        services.AddSingleton<IHybridNodeCollector, HybridNodeCollector>();
        services.AddSingleton<IGpoCollector, GpoCollector>();

        return services.BuildServiceProvider();
    }

    private static void Run(ServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Сбор AD информации
        var adCollector = serviceProvider.GetRequiredService<IAdCollector>();
        DomainInfo? adInfo = CollectAdInfo(adCollector, logger);

        // Сбор системной информации
        var systemCollector = serviceProvider.GetRequiredService<INodeSystemInfoCollector>();
        var systemInfo = CollectSystemInfo(systemCollector, logger);

        // Сбор GPO политик
        var gpoCollector = serviceProvider.GetRequiredService<IGpoCollector>();
        var gpoTree = CollectGpoInfo(gpoCollector, logger);

        // Вывод
        Console.WriteLine("\n=== Active Directory Info ===");
        PrintJson(adInfo);

        Console.WriteLine("\n=== System Hardware Info ===");
        PrintJson(systemInfo);

        Console.WriteLine("\n=== GPO Info ===");
        if (gpoTree == null)
        {
            Console.WriteLine("\n=== NO GPO Info ===");
        }
        else
        {
            PrintJson(gpoTree);

            // Симуляция RSOP
            var simulator = new RsopSimulator();
            var result = simulator.SimulateComputerRsop(
                "CN=PC01,OU=Servers,OU=Corp,DC=laborato,DC=corp",
                gpoTree);

            Console.WriteLine("\n=== RSOP Simulation Info ===");
            PrintJson(result);
        }
    }

    private static DomainInfo? CollectAdInfo(IAdCollector collector, ILogger logger)
    {
        try
        {
            logger.LogInformation("Collecting Active Directory information...");
            var info = collector.Collect();

            if (info == null)
                logger.LogWarning("AD information is not available on this machine.");

            return info;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to collect AD information");
            return null;
        }
    }

    private static NodeSystemInfo CollectSystemInfo(INodeSystemInfoCollector collector, ILogger logger)
    {
        try
        {
            logger.LogInformation("Collecting system hardware information...");
            return collector.Collect();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to collect system hardware info");
            return new NodeSystemInfo();
        }
    }

    private static GpoTopology? CollectGpoInfo(IGpoCollector collector, ILogger logger)
    {
        try
        {
            logger.LogInformation("Collecting system hardware information...");
            return collector.Collect();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to collect system hardware info");
            return null;
        }
    }

    private static void PrintJson<T>(T? obj)
    {
        if (obj == null)
        {
            Console.WriteLine("No data available.");
            return;
        }

        string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Console.WriteLine(json);
    }
}
