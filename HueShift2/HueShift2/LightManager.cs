using HueShift2.Configuration.Model;
using HueShift2.Helpers;
using HueShift2.Interfaces;
using HueShift2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2
{
    public class LightManager : ILightManager
    {
        private ILogger<LightManager> logger;
        private IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private IHueClientManager clientManager;
        private ILocalHueClient client;

        private List<Light> lightsOnNetwork;
        private IDictionary<string, HueShiftLight> hueShiftLights;

        public LightManager(ILogger<LightManager> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, IHueClientManager clientManager, ILocalHueClient client)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.clientManager = clientManager;
            this.client = client;
            this.lightsOnNetwork = new List<Light>();
            this.hueShiftLights = new Dictionary<string, HueShiftLight>();
        }

        #region Light Discovery

        private async Task<List<Light>> DiscoverLights()
        {
            var discoveredLights = (await client.GetLightsAsync()).ToList();
            if (discoveredLights.Count - lightsOnNetwork.Count >= 0)
            {
                logger.LogInformation($"Discovered {discoveredLights.Count - lightsOnNetwork.Count} new lights on the network.");
            }
            else
            {
                logger.LogInformation($"{lightsOnNetwork.Count - discoveredLights.Count} lights are no longer connected to the network.");
                //log names and ids of removed lights
            }
            return discoveredLights;
        }

        private async Task RefreshLights(DateTime currentTime)
        {
            logger.LogDebug("Refreshing lights...");
            await clientManager.AssertConnected();
            this.lightsOnNetwork = await DiscoverLights();
            this.hueShiftLights = hueShiftLights.Trim(lightsOnNetwork);
            var excludedLights = appOptionsDelegate.CurrentValue.LightsToExclude;
            var duration = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.StandardTransitionTime);
            foreach (var light in lightsOnNetwork)
            {
                var isExcluded = excludedLights.Any(x => x == light.Id);
                if (!this.hueShiftLights.ContainsKey(light.Id))
                {
                    var hueShiftLight = new HueShiftLight(light);
                    if (isExcluded)
                    {
                        hueShiftLight.Exclude();
                    }
                    this.hueShiftLights.Add(hueShiftLight.Id, hueShiftLight);
                }
                else
                {
                    var expectedLight = this.hueShiftLights[light.Id];
                    var staleControlState = expectedLight.ControlState;
                    expectedLight.Refresh(light, currentTime);
                    if (staleControlState != expectedLight.ControlState)
                    {
                        logger.LogInformation($"ID: {light.Id,-4} Name: {light.Name,-20} changed from {staleControlState} to {expectedLight.ControlState}");
                    }
                    if (isExcluded)
                    {
                        expectedLight.Exclude();
                    }
                    await Sync(light, expectedLight, currentTime, duration);
                }
            }
            logger.LogDebug("Lights refreshed.");
            return;
        }

        #endregion

        #region Light Control

        private async Task Sync(Light light, HueShiftLight expectedLight, DateTime currentTime, TimeSpan duration)
        {
            if (expectedLight.ControlState == LightControlState.Excluded || expectedLight.ControlState == LightControlState.Manual ||
                expectedLight.State.PowerState == LightPowerState.Off)
            {
                return;
            }
            if (expectedLight.State.Matches(light.State)) return;
            var command = new LightCommand
            {
                TransitionTime = duration,
                ColorCoordinates = expectedLight.State.Colour.ColourCoordinates,
                ColorTemperature = expectedLight.State.Colour.ColourTemperature,
                Hue = expectedLight.State.Colour.Hue,
                Saturation = expectedLight.State.Colour.Saturation,
            };
            logger.LogInformation($"Syncing light | Id: {light.Id} Name: {light.Name} | from: {new LightState(light.State).ToString(true)} | to: {expectedLight.State.ToString(true)}");
            await client.SendCommandAsync(command, new string[] { light.Id });
            return;
        }

        public async Task ExecuteRefresh(DateTime currentTime)
        {
            await RefreshLights(currentTime);
            return;
        }

        public async Task ExecuteTransitionCommand(LightState target, LightCommand command, DateTime currentTime, bool resumeControl)
        {
            await RefreshLights(currentTime);
            var commandIds = this.hueShiftLights.SelectLightsToControl(resumeControl);
            var logMessage = $"Commanding lights:";
            foreach(var id in commandIds)
            {
                logMessage += $"\nID: {id} Name: {this.lightsOnNetwork.First(x => x.Id == id).Name} | from: {hueShiftLights[id].State.ToString(true)} | to: {target.ToString(true)}";
            }
            if (commandIds.Count() > 0)
            {
                await client.SendCommandAsync(command, commandIds);
                foreach (var idLightPair in this.hueShiftLights)
                {
                    if(commandIds.Any(i => i == idLightPair.Key))
                    {
                        idLightPair.Value.TakeControl();
                        idLightPair.Value.ExecuteTransitionCommand(command, currentTime);
                    }
                    else
                    {
                        idLightPair.Value.ExecuteInstantaneousCommand(command);
                    }
                }
            }
            return;
        }

        #endregion

        #region Output

        public void ListAll()
        {
            if (lightsOnNetwork.Count == 0)
            {
                logger.LogWarning("Lights on network: none");
            }
            else
            {
                var logMessage = "Lights on network: \n";
                foreach (var light in lightsOnNetwork)
                {
                    logMessage += $"ID: {light.Id,-4} Name: {light.Name,-20} ModelID: {light.ModelId,-10} ProductID: {light.ProductId,-10} \n";
                }
                logger.LogInformation(logMessage);
            }
            return;
        }

        public void ListExcluded()
        {
            var ids = this.lightsOnNetwork.ToDictionary(x => x.Id, x => x);
            var excludedLightIds = appOptionsDelegate.CurrentValue.LightsToExclude;
            if (excludedLightIds.Length == 0)
            {
                logger.LogInformation("Excluded lights: none");
            }
            else
            {
                var logMessage = "Excluded lights:";
                foreach (var id in excludedLightIds)
                {
                    logMessage += $"\nID: {id} Name: {ids[id].Name}";
                }
                logger.LogInformation(logMessage);
            }
            return;
        }

        public void ListManual()
        {
            var ids = this.lightsOnNetwork.ToDictionary(x => x.Id, x => x);
            var manualLightIds = this.hueShiftLights.Values.Where(x => x.ControlState == LightControlState.Manual)
                .Select(x => x.Id)
                .ToArray();
            if (manualLightIds.Length == 0)
            {
                logger.LogInformation("Manually controlled lights: none");
            }
            else
            {
                var logMessage = "Manually controlled lights:";
                foreach (var id in manualLightIds)
                {
                    logMessage += $"\nID: {id} Name: {ids[id].Name}";
                }
                logger.LogInformation(logMessage);
            }
            return;
        }

        public async Task OutputLightsOnNetwork(DateTime currentTime)
        {
            await RefreshLights(currentTime);
            ListAll();
            ListExcluded();
            ListManual();
        }

        #endregion
    }
}
