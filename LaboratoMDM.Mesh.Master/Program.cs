using LaboratoMDM.Mesh.Master;
using LaboratoMDM.Mesh.Master.Repositories;
using LaboratoMDM.Mesh.Master.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<MasterService>();
        services.AddSingleton<AgentRegistry>();
        services.AddSingleton<INodeInfoRepository, NodeInfoRepository>();
        services.AddGrpc();
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
            });
        });
    })
    .Build();

await host.RunAsync();

