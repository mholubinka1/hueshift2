using HueShift2.Configuration;
using HueShift2.Control;
using HueShift2.Host;
using HueShift2.Configuration.Model;
using HueShift2.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace HueShift2
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var requiredAtStartup = await Startup.AssertConfiguration(args);
            Log.Logger = requiredAtStartup.Item1;
            var lightingConfigFilePath = requiredAtStartup.Item2;
            Log.Information("Starting...");
            try
            {
                var host = new HostBuilder()
                    .ConfigureLogging(logging =>
                    {
                        logging.AddSerilog();
                    })
                    .ConfigureAppConfiguration((hostContext, config) =>
                    {
                        config.SetBasePath(Directory.GetCurrentDirectory());
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                        config.AddJsonFile(lightingConfigFilePath, optional: false, reloadOnChange: true);
                        config.AddEnvironmentVariables();
                        config.AddCommandLine(args);
                    })
                    .ConfigureServices((hostContext, services) =>
                    {                   
                        services.AddOptions();
                        services.Configure<HueShiftOptions>(hostContext.Configuration.GetSection("HueShiftOptions"));
                        services.Configure<CustomScheduleOptions>(hostContext.Configuration.GetSection("CustomScheduleOptions"));
                        services.AddSingleton<ILocalHueClient>(client => new LocalHueClient(hostContext.Configuration["HueShiftOptions:BridgeProperties:IpAddress"]));
                        services.AddSingleton<IConfigFileHelper, ConfigFileHelper>();
                        services.AddSingleton<IHueClientManager, LocalHueClientManager>();
                        services.AddSingleton<ILightManager, LightManager>();
                        services.AddScoped<ITransitionProvider, AutoTransitionProvider>();
                        services.AddScoped<ITransitionProvider, CustomTransitionProvider>();
                        services.AddSingleton<ILightController, AutoLightController>();
                        services.AddSingleton<ILightController, CustomLightController>();
                        services.AddSingleton<ILightScheduler, LightScheduler>();
                        services.AddHostedService<LightSchedulerService>();
                    });
                Log.Information("Host created successfully.");
                await host.Build().RunAsync();
            }
            catch(Exception e)
            {
                Log.Fatal(e, "Host builder failed.");
                throw;
            }
        }
    }
}
