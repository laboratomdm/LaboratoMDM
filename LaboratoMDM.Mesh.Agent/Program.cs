using Grpc.Net.Client;
using LaboratoMDM.Agent.Services;
using LaboratoMDM.Mesh.Agent.Options;
using LaboratoMDM.NodeEngine;
using LaboratoMDM.NodeEngine.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.Versioning;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // Options
        services.Configure<MeshOptions>(
            ctx.Configuration.GetSection(MeshOptions.SectionName));

        services.Configure<AgentOptions>(
            ctx.Configuration.GetSection(AgentOptions.SectionName));

        //Collectors
        services.AddSingleton<INodeSystemInfoCollector, NodeSystemInfoCollector>();
        services.AddSingleton<IUserCollector, UserCollectorService>();
        services.AddSingleton<INodeFullInfoCollector, NodeFullInfoCollector>();

        services.AddSingleton(sp =>
        {
            var meshOptions = sp
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<MeshOptions>>()
                .Value;

            return GrpcChannel.ForAddress(meshOptions.MasterUrl);
        });

        services.AddSingleton<IAgentNodeReporter, AgentNodeReporter>();
        services.AddHostedService<AgentHostedService>();
    })
    .Build();

await host.RunAsync();
