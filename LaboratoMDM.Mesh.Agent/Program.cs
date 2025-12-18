using Grpc.Net.Client;
using LaboratoMDM.Agent.Services;
using LaboratoMDM.NodeEngine;
using LaboratoMDM.NodeEngine.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.Versioning;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<INodeSystemInfoCollector, NodeSystemInfoCollector>();
        services.AddSingleton<IUserCollector, UserCollectorService>();
        services.AddSingleton<INodeFullInfoCollector, NodeFullInfoCollector>();

        services.AddSingleton(sp =>
        {
            var masterUrl = "http://localhost:5000";// ctx.Configuration["Mesh:MasterUrl"];
            return GrpcChannel.ForAddress(masterUrl);
        });

        services.AddSingleton<IAgentNodeReporter, AgentNodeReporter>();
        services.AddHostedService<AgentHostedService>();
    })
    .Build();

await host.RunAsync();
