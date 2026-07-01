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
using System.Linq;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightRegistry : ILightRegistry
    {
        private readonly ILogger<LightRegistry> logger;
        private readonly ILocalHueClient client;

        private readonly Dictionary<string, LightControlPair> lights = new();

        public LightRegistry(ILogger<LightRegistry> logger, ILocalHueClient client)
        {
            this.logger = logger;
            this.client = client;
        }

        public async Task Discover(LightCommand cachedCommand, DateTime currentTime, ColourTemperature ct, TimeSpan syncGracePeriod)
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
                    if (cachedCommand is not null && light.PowerState == LightPowerState.On)
                    {
                        light.ExecuteInstantaneousCommand(cachedCommand);
                        light.MarkForSync();
                    }
                }
                else
                {
                    var light = lights[id];
                    var stale = new CachedControlPair(light);
                    light.Refresh(discoveredLight.State, currentTime, ct.Coolest, ct.Warmest, syncGracePeriod);
                    refreshLog.Add((stale, light));
                }
            }

            var syncConfirmed = refreshLog.Where(p => p.current.SyncConfirmed).ToList();
            var syncFailed = refreshLog.Where(p => p.current.SyncFailed).ToList();
            logger.LogRefresh(refreshLog.Where(p => !p.current.SyncConfirmed && !p.current.SyncFailed));
            logger.LogSyncConfirmed(syncConfirmed);
            logger.LogSyncFailed(syncFailed);

            foreach (var pair in lights)
            {
                if (pair.Key != pair.Value.Properties.Id)
                    throw new InvalidOperationException("Lights must be accessible via the Light ID.");
            }
        }

        public IReadOnlyDictionary<string, LightControlPair> GetAll() => lights;

        public void Reset() => lights.Reset();
    }
}
