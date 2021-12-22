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
    public class StartupManager
    {
        private readonly IConfiguration startupConfig;
        private readonly LightingConfigFileManager lightingConfigFileManager;

        public StartupManager(string[] args)
        {
            startupConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(startupConfig)
                .CreateLogger();

            var fileHelperLogger = new SerilogTypedLogger<ConfigFileHelper>(Log.Logger);
            var configFileHelper = new ConfigFileHelper(fileHelperLogger);
            var fileManagerLogger = new SerilogTypedLogger<LightingConfigFileManager>(Log.Logger);
            lightingConfigFileManager = new LightingConfigFileManager(fileManagerLogger, configFileHelper, startupConfig);


        }

        private async Task GenerateStartupConfigurationFile(IConfiguration config)
        {
            var lightingConfigFilePath = config["config-file"];
            Log.Warning($"{lightingConfigFilePath} does not exist.");
            await lightingConfigFileManager.Generate(lightingConfigFilePath);
            Log.Information($"{lightingConfigFilePath} successfully generated.");
        }

        private async Task AssertGeneratedConfigurationFile(IConfiguration generatedConfig, string configFilePath)
        {
            var bridgeIp = generatedConfig["HueShiftOptions:BridgeProperties:IpAddress"];
            await lightingConfigFileManager.Assert(configFilePath, bridgeIp);
        }

        public async Task<Tuple<ILogger, string>> AssertConfiguration()
        {
            var lightingConfigFilePath = startupConfig["config-file"];
            if (!File.Exists(lightingConfigFilePath))
            {
                await GenerateStartupConfigurationFile(startupConfig);
            }
            else
            {
                var generatedConfig = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(lightingConfigFilePath, optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
                await AssertGeneratedConfigurationFile(generatedConfig, lightingConfigFilePath);
            }   
            return new Tuple<ILogger, string>(Log.Logger, lightingConfigFilePath);
        }
    }
}
