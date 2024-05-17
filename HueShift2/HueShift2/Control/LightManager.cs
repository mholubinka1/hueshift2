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

        private async Task Synchronise(IDictionary<string, LightCommand> syncCommandsPairs)
        {
            foreach (var pair in syncCommandsPairs)
            {
                var id = pair.Key;
                var syncCommand = pair.Value;
                var light = lights[id];
                light.ExecuteInstantaneousCommand(syncCommand);
                logger.LogInformation($"Syncing light | Id: {light.Properties.Id} Name: {light.Properties.Name} | " +
                    $"from: {new AppLightState(light.NetworkLight)} | " +
                    $"to: {light.ExpectedLight}");
                light.ExecuteSync();
                await client.SendCommandAsync(syncCommand, new[] { id });
            }
        }

        public async Task Refresh(DateTime currentTime)
        {
            await clientManager.AssertConnected();
            var discoveredLights = await Discover();

            var syncCommands = new Dictionary<string, LightCommand>();
            foreach (var discoveredLight in discoveredLights)
            {
                var id = discoveredLight.Id;
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
                    light.Refresh(discoveredLight.State, currentTime);
                    logger.LogRefresh(staleLight, light);
                    if (lights[id].RequiresSync(out LightCommand syncCommand))
                    {
                        syncCommand.TransitionTime = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.BasicTransitionDuration);
                        syncCommands.Add(id, syncCommand);
                    }
                }
            }
            if (syncCommands.Any()) await Synchronise(syncCommands);
            foreach(var idLightPair in lights)
            {
                if(idLightPair.Key != idLightPair.Value.Properties.Id)
                {
                    throw new InvalidOperationException("Lights must be accessible via the Light ID.");
                }
            }
            return;
        }

        public async Task Transition(AppLightState target, LightCommand command, DateTime currentTime, bool reset)
        {
            if (reset)
            {
                lights.Reset();
            }
            await Refresh(currentTime);
            PrintScheduled();
            var commandLights = lights.SelectLightsToControl();
            commandLights = commandLights.Filter(target);
            logger.LogCommand(commandLights, target);
            var commandIds = commandLights.Select(x => x.Properties.Id).ToArray();
            foreach (var light in lights.Values)
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
            if (commandIds.Length > 0) await client.SendCommandAsync(command, commandIds);
            cachedLightCommand = command;
            logger.LogUpdate(lights.Values);
            return;
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

        #endregion
    }
}
