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
            var requiredAtStartup = await new StartupManager(args).AssertConfiguration();
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
                        services.AddSingleton<ILocalHueClient>(client => new LocalHueClient(hostContext.Configuration["HueShiftOptions:BridgeProperties:IpAddress"]));
                        services.AddSingleton<IConfigFileHelper, ConfigFileHelper>();
                        services.AddSingleton<IHueClientManager, LocalHueClientManager>();
                        services.AddSingleton<ILightManager, LightManager>();
                        services.AddScoped<ILightColourCalculator, AdaptiveLightColourCalculator>();
                        services.AddScoped<IScheduleProvider, AdaptiveScheduleProvider>();
                        services.AddSingleton<ILightScheduler, AdaptiveLightScheduler>();
                        services.AddSingleton<ILightScheduleWorker, LightScheduleWorker>();
                        services.AddHostedService<LightScheduleService>();
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
