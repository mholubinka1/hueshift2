using HueShift2.Configuration.Model;
using HueShift2.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Q42.HueApi.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HueShift2
{
    public class LocalHueClientManager : IHueClientManager
    {
        private readonly ILogger<LocalHueClientManager> logger;
        private readonly IConfiguration configuration;
        private readonly IOptionsMonitor<HueShiftOptions> optionsDelegate;

        private readonly ILocalHueClient client;
        private readonly IConfigFileHelper configHelper;

        public LocalHueClientManager(ILogger<LocalHueClientManager> logger, IConfiguration configuration, IOptionsMonitor<HueShiftOptions> optionsDelegate, 
            ILocalHueClient client, IConfigFileHelper configHelper)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.optionsDelegate = optionsDelegate;
            this.client = client;
            this.configHelper = configHelper;
        }

        private async Task<string> RegisterApplication(double retryInterval, CancellationToken token)
        {
            logger.LogInformation("Registering application with Hue bridge...");
            var registered = false;
            string apiKey = "";
            while (!registered) {
                token.ThrowIfCancellationRequested();
                try
                {
                    apiKey = await client.RegisterAsync("hueshift-2", "Bridge0");
                    logger.LogInformation("Application registered with bridge.");
                    configHelper.AddOrUpdateSetting(configuration["LightingConfigFilePath"], "HueShiftOptions:BridgeProperties:ApiKey", apiKey);
                    registered = true;
                }
                catch
                {
                    logger.LogWarning("Failed to authorise application. Ensure the button on the top of the bridge has been pressed.");
                    logger.LogInformation($"Retrying in {retryInterval}s.");
                    await Task.Delay(TimeSpan.FromSeconds(retryInterval));
                    logger.LogInformation("Retrying...");
                }
            }
            return apiKey;
        }

        public async Task AssertConnected()
        {
            if (!await client.CheckConnection()){
                if (string.IsNullOrEmpty(optionsDelegate.CurrentValue.BridgeProperties.ApiKey))
                {
                    var cts = new CancellationTokenSource();
                    const int cancelAfter = 120;
                    const double retryInterval = 10.0;
                    try
                    {
                        cts.CancelAfter(cancelAfter * 1000);
                        optionsDelegate.CurrentValue.BridgeProperties.ApiKey = await RegisterApplication(retryInterval, cts.Token);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Application failed to register with bridge: timed out after {cancelAfter}s");
                        throw;
                    }
                }
                try
                {
                    client.Initialize(optionsDelegate.CurrentValue.BridgeProperties.ApiKey);
                    logger.LogInformation("Hue client initialised.");
                }
                catch (Exception e)
                {
                    logger.LogError($"Failed to initialise Hue client", e);
                    throw;
                }
            }
            return;
        }
    }
}
