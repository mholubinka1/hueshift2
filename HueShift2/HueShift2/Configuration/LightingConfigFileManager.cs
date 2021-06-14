using HueShift2.Configuration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models.Bridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Configuration
{
    public class LightingConfigFileManager
    {
        private readonly ILogger logger;

        private readonly IConfigFileHelper configFileHelper;

        private readonly IBridgeLocator bridgeLocator;
        private readonly IGeoLocator geolocator;

        public LightingConfigFileManager(ILogger<LightingConfigFileManager> logger, IConfigFileHelper configFileHelper, IConfiguration configuration)
        {
            this.logger = logger;
            this.configFileHelper = configFileHelper;
            bridgeLocator = new HttpBridgeLocator();
            geolocator = new Geolocator(configuration.GetSection("IpStackApi"));
        }

        private async Task<BridgeProperties> DiscoverBridgesOnNetwork()
        {
            logger.LogInformation($"Searching for Hue bridges on network...");
            //FIXME: what if no bridges are found?
            var locatedBridges = (await bridgeLocator.LocateBridgesAsync(TimeSpan.FromSeconds(30))).ToList();
            var locatedBridge = locatedBridges[0];
            if (locatedBridges.Count() > 1)
            {
                logger.LogWarning($"App does not support more than one bridge.");
            }
            logger.LogInformation($"Hue BridgeID: { locatedBridge.BridgeId,-10} IP: {locatedBridge.IpAddress} found.");
            return new BridgeProperties 
            { 
                IpAddress = locatedBridge.IpAddress 
            };
        }

        private async Task<Geolocation> FindGeolocation()
        {
            logger.LogInformation("Finding geolocation...");
            var geolocation = await geolocator.Get();
            logger.LogInformation($"Located. TZ: {geolocation.TimeZone} " +
                $"Lat: {geolocation.Latitude} " +
                $"Long: {geolocation.Longitude}");
            return geolocation;
        }

        private void WriteConfigToFile(string configFilePath, LightingConfiguration hueShiftOptions)
        {
            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(hueShiftOptions, Formatting.Indented, new StringEnumConverter()));
        }

        public async Task Assert(string configFilePath, string bridgeIp)
        {
            logger.LogInformation($"Asserting {configFilePath} Hue bridge settings...");
            var discoveredBridge = await DiscoverBridgesOnNetwork();
            var discoveredBridgeIp = discoveredBridge.IpAddress;
            if (discoveredBridgeIp != bridgeIp)
            {
                logger.LogWarning($"Hue bridge IP mismatch. Writing updated IP address to configuration file...");
                var key = "HueShiftOptions:BridgeProperties:IpAddress";
                configFileHelper.AddOrUpdateSetting(configFilePath, key, discoveredBridgeIp);
                logger.LogInformation($"Updated Hue bridge IP address.");
            }
            logger.LogInformation($"Settings confirmed.");
        }

        public async Task Generate(string configFilePath)
        {
            logger.LogInformation($"Generating {configFilePath} with required settings...");
            var bridgeProperties = await DiscoverBridgesOnNetwork();
            var geolocation = await FindGeolocation();
            var hueShiftOptions = new HueShiftOptions
            {
                BridgeProperties = bridgeProperties,
                Geolocation = geolocation,
            };
            hueShiftOptions.SetDefaults();
            WriteConfigToFile(configFilePath, new LightingConfiguration(hueShiftOptions));
            return;
        }
    }
}
