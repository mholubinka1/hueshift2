using HueShift2.Configuration.Model;
using HueShift2.Helpers;
using HueShift2.Interfaces;
using HueShift2.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightManager : ILightManager
    {
        private ILogger<LightManager> logger;
        private IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private IHueClientManager clientManager;
        private ILocalHueClient client;

        private IDictionary<string, LightMemoryPair> lights;

        public LightManager(ILogger<LightManager> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, IHueClientManager clientManager, ILocalHueClient client)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.clientManager = clientManager;
            this.client = client;
            this.lights = new Dictionary<string, LightMemoryPair>();
        }

        private async Task<IList<Light>> Discover()
        {
            var discoveredLights = (await client.GetLightsAsync()).ToList();
            logger.LogDiscovery(discoveredLights, lights.Values.Select(x => x.CachedLight).ToList());
            return discoveredLights;
        }

        private void Exclude()
        {
            var excludedLights = appOptionsDelegate.CurrentValue.LightsToExclude;
            foreach (var id in excludedLights)
            {
                lights[id].Exclude();
            }
        }

        public async Task<IDictionary<string, LightMemoryPair>> Refresh(DateTime currentTime)
        {
            await clientManager.AssertConnected();
            var discoveredLights = await Discover();
            var excludedLights = appOptionsDelegate.CurrentValue.LightsToExclude;
            lights = lights.Trim(discoveredLights);
            var syncCommands = new Dictionary<string, LightCommand>();
            foreach (var discoveredLight in discoveredLights)
            {
                var id = discoveredLight.Id;
                var isExcluded = excludedLights.Any(x => x == light.Id);
                if (!lights.ContainsKey(id))
                {
                    var light = new LightMemoryPair(discoveredLight);
                    if (isExcluded) light.Exclude();
                    lights.Add(id, light);
                }
                else
                {
                    
                    lights[id].Refresh(discoveredLight, currentTime);
                    if (isExcluded)
                    {
                        lights[id].Exclude();
                    }
                    else
                    {
                        LightCommand syncCommand;
                        if (lights[id].RequiresSync(out syncCommand))
                        {
                            syncCommands.Add(id, syncCommand);
                        }
                    }
                }
            }
            if (syncCommands.Any()) Synchronise(syncCommands);
            return lights;
        }

        public async Task Synchronise(IDictionary<string, LightCommand> syncCommands)
        {
            foreach (var command in commands)
            {
                await client.SendCommandAsync(command.Content, command.Ids);
            }
        }

        public asnyc Task Transition()
        {

        }
    }S
}
