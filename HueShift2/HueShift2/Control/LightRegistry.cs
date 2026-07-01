using HueShift2.Configuration.Model;
using HueShift2.Helpers;
using HueShift2.Interfaces;
using HueShift2.Logging;
using HueShift2.Model;
using Microsoft.Extensions.Logging;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightRegistry : ILightRegistry
    {
        private readonly ILogger<LightRegistry> logger;
        private readonly ILocalHueClient client;

        private readonly Dictionary<string, LightControlPair> lights = new();
        private readonly ReadOnlyDictionary<string, LightControlPair> readOnlyView;

        public LightRegistry(ILogger<LightRegistry> logger, ILocalHueClient client)
        {
            this.logger = logger;
            this.client = client;
            readOnlyView = new ReadOnlyDictionary<string, LightControlPair>(lights);
        }

        public async Task Discover(LightCommand cachedCommand, DateTime currentTime, ColourTemperature ct)
        {
            var discoveredLights = (await client.GetLightsAsync()).ToList();
            logger.LogDiscovery(discoveredLights, lights.Values);

            var refreshLog = new List<(CachedControlPair stale, LightControlPair current)>();
            foreach (var discoveredLight in discoveredLights)
            {
                var id = discoveredLight.Id;
                if (!lights.ContainsKey(id))
                {
                    var light = new LightControlPair(discoveredLight);
                    lights.Add(id, light);
                    if (cachedCommand is not null)
                    {
                        light.ExecuteInstantaneousCommand(cachedCommand);
                        if (light.PowerState == LightPowerState.On)
                            light.MarkForSync();
                    }
                }
                else
                {
                    var light = lights[id];
                    var stale = new CachedControlPair(light);
                    light.Refresh(discoveredLight.State, currentTime, ct.Coolest, ct.Warmest);
                    refreshLog.Add((stale, light));
                }
            }

            logger.LogRefresh(refreshLog);

            foreach (var pair in lights)
            {
                if (pair.Key != pair.Value.Properties.Id)
                    throw new InvalidOperationException("Lights must be accessible via the Light ID.");
            }
        }

        public IReadOnlyDictionary<string, LightControlPair> GetAll() => readOnlyView;

        public void Reset() => lights.Reset();
    }
}
