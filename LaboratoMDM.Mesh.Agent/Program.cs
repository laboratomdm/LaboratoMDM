using Grpc.Net.Client;
using LaboratoMDM.Agent.Services;
using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.Mesh.Agent.Domain;
using LaboratoMDM.Mesh.Agent.Grpc;
using LaboratoMDM.Mesh.Agent.Options;
using LaboratoMDM.Mesh.Agent.Persistance;
using LaboratoMDM.Mesh.Agent.Persistance.Abstractions;
using LaboratoMDM.Mesh.Agent.Persistance.Mapping;
using LaboratoMDM.Mesh.Agent.Services;
using LaboratoMDM.NodeEngine;
using LaboratoMDM.NodeEngine.Implementations;
using LaboratoMDM.PolicyEngine;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Implementations;
using LaboratoMDM.PolicyEngine.Persistence;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using LaboratoMDM.PolicyEngine.Persistence.Schema;
using LaboratoMDM.PolicyEngine.Services;
using LaboratoMDM.PolicyEngine.Services.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

string dbFile = "agent.db";
string migrationsPath = @"C:\Users\Ivan\source\repos\LaboratoMDM\LaboratoMDM.Mesh.Agent\Database";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // Options
        services.Configure<MeshOptions>(
            ctx.Configuration.GetSection(MeshOptions.SectionName));

        services.Configure<AgentOptions>(
            ctx.Configuration.GetSection(AgentOptions.SectionName));

        // Collectors
        services.AddSingleton<INodeSystemInfoCollector, NodeSystemInfoCollector>();
        services.AddSingleton<IUserCollector, UserCollectorService>();
        services.AddSingleton<INodeFullInfoCollector, NodeFullInfoCollector>();

        // Policy Services
        services.AddSingleton<IPolicyCommandService, PolicyCommandService>();
        services.AddSingleton<IPolicyPlanner, PolicyPlanner>();

        services.AddSingleton<IPolicyApplicabilityChecker<PolicyDefinition>, AdmxApplicabilityChecker>();
        services.AddSingleton<ISupportedOnResolver, SupportedOnResolver>();
        services.AddSingleton<ISupportedOnCatalog, InMemorySupportedOnCatalog>();

        services.AddSingleton<SqliteConnection>(sp =>
        {
            var conn = new SqliteConnection($"Data Source={dbFile}");
            conn.Open();
            return conn;
        });

        services.AddSingleton<IEntityMapper<AgentPolicyComplianceEntity>, AgentPolicyComplianceMapper>();
        services.AddSingleton<IAgentPolicyRepository, AgentPolicyRepository>();
        services.AddSingleton<IAgentPolicyService, AgentPolicyService>();

        services.AddSingleton<IEntityMapper<PolicyEntity>, PolicyEntityMapper>();
        services.AddSingleton<IEntityMapper<PolicyShortView>, PolicyShortViewMapper>();
        services.AddSingleton<IEntityMapper<PolicyDetailsView>, PolicyDetailsViewMapper>();
        services.AddSingleton<IPolicyRepository, PolicyRepository>();
        services.AddSingleton<IPolicyQueryService, PolicyQueryService>();

        services.AddSingleton<IPolicyApplier, RegistryPolicyApplier>();

        //Sync
        services.AddSingleton<AgentPolicySyncClient>();

        services.AddSingleton<AgentPolicySyncRepository>(sp =>
        {
            var repo = new AgentPolicySyncRepository(dbFile);
            return repo;
        });
        services.AddSingleton<AgentPolicySyncService>();

        // === gRPC CLIENT (agent -> master) ===
        services.AddSingleton(sp =>
        {
            var meshOptions = sp
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<MeshOptions>>()
                .Value;

            return GrpcChannel.ForAddress(meshOptions.MasterUrl);
        });

        services.AddSingleton<IAgentNodeReporter, AgentNodeReporter>();
        services.AddHostedService<AgentHostedService>();
        services.AddHostedService<AgentPolicyHostedService>();

        // === gRPC SERVER (master -> agent) ===
        services.AddGrpc();

        // Agent gRPC services
        services.AddSingleton<PolicyAgentServiceImpl>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(6000, listenOptions =>
            {
                listenOptions.Protocols =
                    Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
            });
        });

        webBuilder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<PolicyAgentServiceImpl>();
            });
        });
    })
    .Build();

// Инициализация БД агента
var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("DatabaseInitializer");

await DatabaseInitializer.InitializeAsync(dbFile, migrationsPath, logger);

await host.RunAsync();
