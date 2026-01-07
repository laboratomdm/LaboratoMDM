using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.Mesh.Master.Grpc;
using LaboratoMDM.Mesh.Master.Grpc.Services;
using LaboratoMDM.Mesh.Master.Repositories;
using LaboratoMDM.Mesh.Master.Services;
using LaboratoMDM.Mesh.Master.Services.Abstractions;
using LaboratoMDM.PolicyEngine.Domain;
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
using StackExchange.Redis;

//string dbFile = "master.db";
//string migrationsPath = @"C:\Users\Ivan\source\repos\LaboratoMDM";

var dbFile =
    Environment.GetEnvironmentVariable("DB_FILE")
    ?? Path.Combine(AppContext.BaseDirectory, "data", "master.db");

var migrationsPath =
    Environment.GetEnvironmentVariable("MIGRATIONS_PATH")
    ?? Path.Combine(AppContext.BaseDirectory, "migrations");

Directory.CreateDirectory(Path.GetDirectoryName(dbFile)!);

// Создаем файл базы, если не существует
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect("redis:6379"));

        services.AddSingleton<IAgentRegistry, RedisAgentRegistry>();
        services.AddSingleton<INodeInfoRepository, RedisNodeInfoRepository>();

        services.AddSingleton<MasterService>();
        services.AddSingleton<AgentServiceImpl>();
        services.AddSingleton<UserServiceImpl>();
        services.AddSingleton<AdmxServiceImpl>();
        services.AddSingleton<PolicyCatalogServiceImpl>();
        services.AddSingleton<PolicyAssignmentServiceImpl>();
        // Sync
        services.AddSingleton<PolicySyncServiceImpl>();
        services.AddSingleton<AgentPayloadBuilder>(sp =>
        {
            var builder = new AgentPayloadBuilder(dbFile);
            return builder;
        });

        services.AddGrpc();

        services.AddSingleton<SqliteConnection>(sp =>
        {
            var conn = new SqliteConnection($"Data Source={dbFile}");
            conn.Open();
            return conn;
        });

        // Mappers
        services.AddSingleton<IEntityMapper<AdmxFileEntity>, AdmxFileEntityMapper>();
        services.AddSingleton<IEntityMapper<PolicyNamespaceEntity>, PolicyNamespaceEntityMapper>();
        services.AddSingleton<IEntityMapper<PolicyCategoryEntity>, PolicyCategoryEntityMapper>();
        services.AddSingleton<IEntityMapper<IReadOnlyList<PolicyCategoryView>>, PolicyCategoryViewMapper>();
        services.AddSingleton<IEntityMapper<PolicyEntity>, PolicyEntityMapper>();
        services.AddSingleton<IEntityMapper<PolicyDetailsView>, PolicyDetailsViewMapper>();
        services.AddSingleton<IEntityMapper<Translation>, TranslationEntityMapper>();

        // Repositories
        services.AddSingleton<IAdmxRepository, AdmxRepository>();
        services.AddSingleton<IPolicyMetadataRepository, PolicyMetadataRepository>();
        services.AddSingleton<IPolicyRepository, PolicyRepository>();
        services.AddSingleton<ITranslationRepository, TranslationRepository>();
        services.AddSingleton<IAdmlSnapshotWriter, AdmlSnapshotWriter>();

        // Business Services
        services.AddSingleton<IAdmxImportService, AdmxImportService>();
        services.AddSingleton<IAdmxQueryService, AdmxQueryService>();
        services.AddSingleton<IPolicyQueryService, PolicyQueryService>();
        services.AddSingleton<IPolicyMetadataService, PolicyMetadataService>();

        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<IAdmlImportService, AdmlImportService>();
        services.AddSingleton<IAdmlPresentationImportService, AdmlPresentationImportService>();

        services.AddSingleton<IAdmxFolderImporter, AdmxFolderImporter>();
        services.AddSingleton<IAdmlFolderImporter, AdmlFolderImporter>();

        services.AddSingleton<IPolicyAssignService, PolicyAssignService>();
        services.AddSingleton<IAgentPolicyClient, AgentPolicyClient>();

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
            options.ListenAnyIP(5000, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
            });
        });

        webBuilder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<MasterService>();
                endpoints.MapGrpcService<AgentServiceImpl>();
                endpoints.MapGrpcService<UserServiceImpl>();
                endpoints.MapGrpcService<AdmxServiceImpl>();
                endpoints.MapGrpcService<PolicyCatalogServiceImpl>();
                endpoints.MapGrpcService<PolicySyncServiceImpl>();
                endpoints.MapGrpcService<PolicyAssignmentServiceImpl>();
            });
        });
    })
    .Build();

// Инициализация базы данных перед стартом хоста
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
await DatabaseInitializer.InitializeAsync(dbFile, migrationsPath, logger);

await host.RunAsync();