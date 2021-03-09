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
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightManager : ILightManager
    {
        private ILogger<LightManager> logger;
        private IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private IHueClientManager clientManager;
        private ILocalHueClient client;

        private List<Light> lightsOnNetwork;
        private IDictionary<string, HueShiftLight> lightsInMemory;

        public LightManager(ILogger<LightManager> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, IHueClientManager clientManager, ILocalHueClient client)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.clientManager = clientManager;
            this.client = client;
            this.lightsOnNetwork = new List<Light>();
            this.lightsInMemory = new Dictionary<string, HueShiftLight>();
        }

        #region Light Discovery

        private async Task<List<Light>> DiscoverLights()
        {
            var discoveredLights = (await client.GetLightsAsync()).ToList();
            if (discoveredLights.Count - lightsOnNetwork.Count >= 0)
            {
                if(discoveredLights.Count - lightsOnNetwork.Count == 0)
                {
                    logger.LogDebug($"Discovered {discoveredLights.Count - lightsOnNetwork.Count} new lights on the network.");
                }
                else
                {
                    logger.LogInformation($"Discovered {discoveredLights.Count - lightsOnNetwork.Count} new lights on the network.");
                }
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
            this.lightsInMemory = this.lightsInMemory.Trim(lightsOnNetwork);
            var excludedLights = appOptionsDelegate.CurrentValue.LightsToExclude;
            var duration = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.StandardTransitionTime);
            foreach (var light in lightsOnNetwork)
            {
                var isExcluded = excludedLights.Any(x => x == light.Id);
                if (!this.lightsInMemory.ContainsKey(light.Id))
                {
                    var hueShiftLight = new HueShiftLight(light);
                    if (isExcluded)
                    {
                        hueShiftLight.Exclude();
                    }
                    this.lightsInMemory.Add(hueShiftLight.Id, hueShiftLight);
                }
                else
                {
                    var expectedLight = this.lightsInMemory[light.Id];
                    var stalePowerState = expectedLight.State.PowerState;
                    var staleControlState = expectedLight.ControlState;
                    expectedLight.Refresh(light, currentTime);
                    if (stalePowerState != expectedLight.State.PowerState)
                    {
                        logger.LogInformation($"ID: {light.Id,-4} Name: {light.Name,-20} | Power state changed from {stalePowerState} to {expectedLight.State.PowerState}");
                    }
                    if (staleControlState != expectedLight.ControlState)
                    {
                        logger.LogInformation($"ID: {light.Id,-4} Name: {light.Name,-20} | Control state changed from {staleControlState} to {expectedLight.ControlState}");
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
                expectedLight.State.PowerState == LightPowerState.Off || expectedLight.State.PowerState == LightPowerState.Transitioning)
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
            logger.LogInformation($"Syncing light | Id: {light.Id} Name: {light.Name} | from: {new HueShiftLightState(light.State).ToString(true)} | to: {expectedLight.State.ToString(true)}");
            await client.SendCommandAsync(command, new string[] { light.Id });
            return;
        }

        public async Task ExecuteRefresh(DateTime currentTime)
        {
            await RefreshLights(currentTime);
            return;
        }

        public async Task ExecuteTransitionCommand(HueShiftLightState target, LightCommand command, DateTime currentTime, bool resumeControl)
        {
            await RefreshLights(currentTime);
            var commandIds = this.lightsInMemory.SelectLightsToControl(resumeControl);
            var logCommandMessage = $"Commanding lights:";
            foreach(var id in commandIds)
            {
                logCommandMessage += $"\nID: {id} Name: {this.lightsOnNetwork.First(x => x.Id == id).Name} | from: {this.lightsInMemory[id].State.ToString(true)} | to: {target.ToString(true)}";
            }
            logger.LogInformation(logCommandMessage);
            if (commandIds.Count() > 0) await client.SendCommandAsync(command, commandIds);
            var logExpectedMessage = $"New light states in memory:";
            foreach (var idLightPair in this.lightsInMemory)
            {
                if (commandIds.Any(i => i == idLightPair.Key))
                {
                    idLightPair.Value.TakeControl();
                    idLightPair.Value.ExecuteTransitionCommand(command, currentTime);
                }
                else
                {
                    idLightPair.Value.ExecuteInstantaneousCommand(command);
                }
                logExpectedMessage += $"\nID: {idLightPair.Key} Name: {this.lightsOnNetwork.First(x => x.Id == idLightPair.Key).Name} | {idLightPair.Value.State.ToString(true)}";
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
                var logMessage = "Lights on network:";
                foreach (var light in lightsOnNetwork)
                {
                    logMessage += $"\nID: {light.Id,-4} Name: {light.Name,-20} ModelID: {light.ModelId,-10} ProductID: {light.ProductId,-10}";
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
            var manualLightIds = this.lightsInMemory.Values.Where(x => x.ControlState == LightControlState.Manual)
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
