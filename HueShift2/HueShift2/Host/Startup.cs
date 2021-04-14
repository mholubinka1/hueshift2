using HueShift2.Configuration;
using HueShift2.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2
{
    public static class Startup
    {
        private static IConfiguration BuildConfiguration(string[] args)
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
        }

        private static async Task GenerateStartupConfigurationFile(IConfiguration config, SerilogTypedLogger<LightingConfigFileGenerator> logger)
        {
            var lightingConfigFilePath = config["config-file"];
            Log.Warning($"{lightingConfigFilePath} does not exist.");
            await new LightingConfigFileGenerator(logger, config).Generate(lightingConfigFilePath);
            Log.Information($"{lightingConfigFilePath} successfully generated.");
        }

        public static async Task<Tuple<ILogger, string>> AssertConfiguration(string[] args)
        {
            var startupConfig = BuildConfiguration(args);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(startupConfig)
                .CreateLogger();

            var lightingConfigFilePath = startupConfig["config-file"];
            if (!File.Exists(lightingConfigFilePath))
            {
                await GenerateStartupConfigurationFile(startupConfig, new SerilogTypedLogger<LightingConfigFileGenerator>(Log.Logger));
            }
            return new Tuple<ILogger, string>(Log.Logger, lightingConfigFilePath);
        }
    }
}
