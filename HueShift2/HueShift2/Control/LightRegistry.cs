using HueShift2.Configuration.Model;
using HueShift2.Helpers;
using HueShift2.Interfaces;
using HueShift2.Logging;
using HueShift2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private readonly Dictionary<string, LightControlPair> lights = new();
        private readonly ReadOnlyDictionary<string, LightControlPair> readOnlyView;
        private readonly HashSet<string> warnedExclusions = new();

        public LightRegistry(ILogger<LightRegistry> logger, ILocalHueClient client, IOptionsMonitor<HueShiftOptions> appOptionsDelegate)
        {
            this.logger = logger;
            this.client = client;
            this.appOptionsDelegate = appOptionsDelegate;
            readOnlyView = new ReadOnlyDictionary<string, LightControlPair>(lights);
        }

        public async Task Discover(LightCommand cachedCommand, DateTime currentTime, ColourTemperature ct)
        {
            var discoveredLights = (await client.GetLightsAsync()).ToList();
            logger.LogDiscovery(discoveredLights, lights.Values);

            var exclusions = new HashSet<string>(appOptionsDelegate.CurrentValue.LightsToExclude ?? Enumerable.Empty<string>());
            var currentNames = discoveredLights.ToDictionary(l => l.Id, l => l.Name);

            var refreshLog = new List<(CachedControlPair stale, LightControlPair current)>();
            foreach (var discoveredLight in discoveredLights)
            {
                var id = discoveredLight.Id;
                var isExcluded = exclusions.Contains(id) || exclusions.Contains(discoveredLight.Name);

                if (!lights.ContainsKey(id))
                {
                    var light = new LightControlPair(discoveredLight);
                    lights.Add(id, light);
                    if (!isExcluded && cachedCommand is not null)
                    {
                        light.UpdateExpectedState(cachedCommand);
                        if (light.IsOn())
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

                var light = pair.Value;
                var currentName = currentNames.TryGetValue(light.Properties.Id, out var name) ? name : light.Properties.Name;
                var isExcluded = exclusions.Contains(light.Properties.Id) || exclusions.Contains(currentName);

                if (isExcluded && light.AppControlState != LightControlState.Excluded)
                    light.Exclude();
                else if (!isExcluded && light.AppControlState == LightControlState.Excluded)
                    light.Unexclude();
            }

            foreach (var entry in exclusions)
            {
                var matched = lights.Values.Any(l =>
                {
                    var currentName = currentNames.TryGetValue(l.Properties.Id, out var name) ? name : l.Properties.Name;
                    return l.Properties.Id == entry || currentName == entry;
                });
                if (!matched && warnedExclusions.Add(entry))
                    logger.LogWarning($"[Exclusion] No light found matching '{entry}' — entry will be ignored.");
            }
        }

        public IReadOnlyDictionary<string, LightControlPair> GetAll() => readOnlyView;

        public void Reset() => lights.Reset();
    }
}
