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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightManager : ILightManager
    {
        private readonly ILogger<LightManager> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private readonly IHueClientManager clientManager;
        private readonly ILocalHueClient client;

        private readonly IDictionary<string, LightControlPair> lights;

        private LightCommand cachedLightCommand;

        public LightManager(ILogger<LightManager> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, IHueClientManager clientManager, ILocalHueClient client)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.clientManager = clientManager;
            this.client = client;
            lights = new Dictionary<string, LightControlPair>();
        }

        private async Task<IList<Light>> Discover()
        {
            var discoveredLights = (await client.GetLightsAsync()).ToList();
            logger.LogDiscovery(discoveredLights, lights.Values);
            return discoveredLights;
        }

        private async Task Synchronise(IDictionary<string, LightCommand> syncCommandsPairs, DateTime currentTime)
        {
            var duration = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.BasicTransitionDuration);
            var lightNames = syncCommandsPairs.Keys.Select(id => lights[id].Properties.Name);
            foreach (var pair in syncCommandsPairs)
            {
                var id = pair.Key;
                var syncCommand = pair.Value;
                var light = lights[id];
                syncCommand.TransitionTime = duration;
                light.ExecuteInstantaneousCommand(syncCommand);
                light.ExecuteSync(duration, currentTime);
                await client.SendCommandAsync(syncCommand, new[] { id });
            }
            logger.LogSync(lightNames, lights[syncCommandsPairs.Keys.First()].ExpectedLight);
        }

        public async Task Refresh(DateTime currentTime)
        {
            await clientManager.AssertConnected();
            var discoveredLights = await Discover();
            var ct = appOptionsDelegate.CurrentValue.ColourTemperature;

            var syncCommands = new Dictionary<string, LightCommand>();
            var refreshLog = new List<(CachedControlPair stale, LightControlPair current)>();
            foreach (var discoveredLight in discoveredLights)
            {
                var id = discoveredLight.Id;
                if (discoveredLight.State.ColorMode == "hs")
                    logger.LogWarning("[Refresh] Light {Id} ({Name}) reports HS colour mode — not supported, treating as None.", id, discoveredLight.Name);
                if (!lights.ContainsKey(id))
                {
                    var light = new LightControlPair(discoveredLight);
                    lights.Add(id, light);
                    if (cachedLightCommand is not null)
                    {
                        var syncCommand = cachedLightCommand;
                        syncCommand.TransitionTime = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.BasicTransitionDuration);
                        syncCommands.Add(id, syncCommand);
                    }
                }
                else
                {
                    var light = lights[id];
                    var staleLight = new CachedControlPair(light);
                    light.Refresh(discoveredLight.State, currentTime, ct.Coolest, ct.Warmest);
                    refreshLog.Add((staleLight, light));
                    if (light.PowerState != LightPowerState.Syncing && light.PowerState != LightPowerState.Transitioning)
                    {
                        if (light.RequiresSync(out LightCommand syncCommand))
                            syncCommands.Add(id, syncCommand);
                    }
                }
            }
            logger.LogRefresh(refreshLog);
            if (syncCommands.Any()) await Synchronise(syncCommands, currentTime);
            foreach (var idLightPair in lights)
            {
                if (idLightPair.Key != idLightPair.Value.Properties.Id)
                {
                    throw new InvalidOperationException("Lights must be accessible via the Light ID.");
                }
            }
            return;
        }

        public async Task Transition(AppLightState target, LightCommand command, DateTime currentTime, bool reset, TransitionType transitionType)
        {
            if (reset) lights.Reset();
            await Refresh(currentTime);
            PrintScheduled();
            var hueShiftOnLights = lights.SelectLightsToControl();
            var commandLights = hueShiftOnLights.Filter(target);
            var commandIds = new HashSet<string>(commandLights.Select(x => x.Properties.Id));
            if (commandIds.Any())
                logger.LogTransition(commandLights, target, transitionType);
            else if (transitionType == TransitionType.Adaptive)
                logger.LogDebug("[Adaptive] Checked — no lights require a colour update.");
            foreach (var light in lights.Values)
            {
                if (commandIds.Contains(light.Properties.Id))
                    light.ExecuteCommand(command, currentTime, transitionType);
                else
                    light.ExecuteInstantaneousCommand(command);
            }
            if (commandIds.Any())
                await client.SendCommandAsync(command, commandIds.ToArray());
            cachedLightCommand = command;
            logger.LogUpdate(lights.Values);
        }

        #region Output

        public void PrintAll()
        {
            PrintScheduled();
            PrintManual();
        }

        public void PrintScheduled()
        {
            var controlledLights = lights.Values.Where(x => x.AppControlState == LightControlState.HueShift).ToArray();
            if (controlledLights.Length == 0)
            {
                logger.LogWarning("Lights under app control: none");
            }
            else
            {
                logger.LogDebug("Lights under app control:");
                logger.LogLightProperties(controlledLights);
            }
        }

        public void PrintManual()
        {
            var manualLights = lights.Values.Where(x => x.AppControlState == LightControlState.Manual).ToArray();
            if (manualLights.Length == 0)
            {
                logger.LogDebug("Manually controlled lights: none");
            }
            else
            {
                logger.LogDebug("Manually controlled lights:");
                logger.LogLightProperties(manualLights);
            }
        }

        #endregion
    }
}
