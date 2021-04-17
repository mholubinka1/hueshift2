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
        private ILogger<LightManager> logger;
        private IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private IHueClientManager clientManager;
        private ILocalHueClient client;

        private IDictionary<string, LightControlPair> lights;

        public LightManager(ILogger<LightManager> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, IHueClientManager clientManager, ILocalHueClient client)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.clientManager = clientManager;
            this.client = client;
            this.lights = new Dictionary<string, LightControlPair>();
        }

        private async Task<IList<Light>> Discover()
        {
            var discoveredLights = (await client.GetLightsAsync()).ToList();
            logger.LogDiscovery(discoveredLights, lights.Values);
            Exclude();
            return discoveredLights;
        }

        private void Exclude()
        {
            var excludedLights = appOptionsDelegate.CurrentValue.LightsToExclude;
            foreach (var id in excludedLights)
            {
                lights[id].Exclude(true);
            }
        }

        private async Task Synchronise(IDictionary<string, LightCommand> syncCommands)
        {
            foreach (var command in syncCommands)
            {
                var light = this.lights[command.Key];
                logger.LogInformation($"Syncing light | Id: {light.Properties.Id} Name: {light.Properties.Name} | " +
                    $"from: {new AppLightState(light.NetworkLight)} | " +
                    $"to: {light.ExpectedLight}");
                await client.SendCommandAsync(command.Value, new[] { command.Key });
            }
        }

        public async Task Refresh(DateTime currentTime)
        {
            await clientManager.AssertConnected();
            var discoveredLights = await Discover();
            var excludedLights = appOptionsDelegate.CurrentValue.LightsToExclude;
            var syncCommands = new Dictionary<string, LightCommand>();
            this.lights = this.lights.Trim(discoveredLights);
            foreach (var discoveredLight in discoveredLights)
            {
                var id = discoveredLight.Id;
                var isExcluded = excludedLights.Any(x => x == discoveredLight.Id);
                if (!this.lights.ContainsKey(id))
                {
                    var light = new LightControlPair(discoveredLight);
                    light.Exclude(isExcluded);
                    this.lights.Add(id, light);
                }
                else
                {
                    var light = this.lights[id];
                    var staleProperties = new Tuple<LightPowerState, LightControlState, bool>(light.PowerState, light.AppControlState, light.ResetOccurred);
                    light.Refresh(discoveredLight.State, currentTime);
                    light.Exclude(isExcluded);
                    logger.LogRefresh(staleProperties, light);
                    if (this.lights[id].RequiresSync(out LightCommand syncCommand))
                    {
                        syncCommand.TransitionTime = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.StandardTransitionTime);
                        syncCommands.Add(id, syncCommand);
                    }
                }
            }
            if (syncCommands.Any()) await Synchronise(syncCommands);
            foreach(var idLightPair in lights)
            {
                if(idLightPair.Key != idLightPair.Value.Properties.Id)
                {
                    throw new InvalidOperationException();
                }
            }
            return;
        }

        public async Task Transition(AppLightState target, LightCommand command, DateTime currentTime, bool reset)
        {
            if (reset)
            {
                this.lights.Reset();
            }
            await Refresh(currentTime);
            PrintScheduled();
            var commandLights = this.lights.SelectLightsToControl();
            commandLights = commandLights.Filter(target);
            logger.LogCommand(commandLights, target);
            var commandIds = commandLights.Select(x => x.Properties.Id).ToArray();
            if (commandIds.Length > 0) await client.SendCommandAsync(command, commandIds);
            foreach (var light in this.lights.Values)
            {
                if(commandIds.Any(i => i == light.Properties.Id))
                {
                    light.ExecuteCommand(command, currentTime);
                }
                else
                {
                    light.ExecuteInstantaneousCommand(command);
                }
            }
            logger.LogUpdate(this.lights.Values);
            return;
        }

        #region Output

        public void PrintAll()
        {
            PrintScheduled();
            PrintManual();
            PrintExcluded();
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
                logger.LogInformation("Lights under app control:");
                logger.LogLightProperties(controlledLights);
            }
        }

        public void PrintManual()
        {
            var manualLights = lights.Values.Where(x => x.AppControlState == LightControlState.Manual).ToArray();
            if (manualLights.Length == 0)
            {
                logger.LogInformation("Manually controlled lights: none");
            }
            else
            {
                logger.LogInformation("Manually controlled lights:");
                logger.LogLightProperties(manualLights);
            }
        }

        public void PrintExcluded()
        {
            var excludedIds = appOptionsDelegate.CurrentValue.LightsToExclude;
            if (excludedIds.Length == 0)
            {
                logger.LogInformation("Excluded lights: none");
            }
            else
            {
                var excludedLights = lights.Values.Where(x => excludedIds.Contains(x.Properties.Id));
                logger.LogInformation("Excluded lights:");
                logger.LogLightProperties(excludedLights);
            }
            return;
        }

        #endregion
    }
}
