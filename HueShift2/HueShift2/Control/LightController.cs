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
using System.Linq;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightController : ILightController
    {
        private readonly ILogger<LightController> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;
        private readonly IHueClientManager clientManager;
        private readonly ILocalHueClient client;
        private readonly ILightRegistry registry;
        private readonly ILightSynchroniser synchroniser;

        private LightCommand cachedLightCommand;

        public LightController(
            ILogger<LightController> logger,
            IOptionsMonitor<HueShiftOptions> appOptionsDelegate,
            IHueClientManager clientManager,
            ILocalHueClient client,
            ILightRegistry registry,
            ILightSynchroniser synchroniser)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.clientManager = clientManager;
            this.client = client;
            this.registry = registry;
            this.synchroniser = synchroniser;
        }

        public async Task Refresh(DateTime currentTime)
        {
            await clientManager.AssertConnected();
            var options = appOptionsDelegate.CurrentValue;
            var ct = options.ColourTemperature;
            await registry.Discover(cachedLightCommand, currentTime, ct);
            await synchroniser.Synchronise(registry.GetAll(), currentTime);
        }

        public async Task Transition(AppLightState target, LightCommand command, DateTime currentTime, bool reset, TransitionType transitionType)
        {
            if (reset) registry.Reset();
            await Refresh(currentTime);
            PrintScheduled();
            var lights = registry.GetAll();
            var hueShiftOnLights = lights.Values
                .Where(l => l.AppControlState == LightControlState.HueShift && l.PowerState == LightPowerState.On)
                .ToArray();
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

        public void PrintAll()
        {
            PrintScheduled();
            PrintManual();
        }

        public void PrintScheduled()
        {
            var controlled = registry.GetAll().Values.Where(x => x.AppControlState == LightControlState.HueShift).ToArray();
            if (controlled.Length == 0)
                logger.LogWarning("Lights under app control: none");
            else
            {
                logger.LogDebug("Lights under app control:");
                logger.LogLightProperties(controlled);
            }
        }

        public void PrintManual()
        {
            var manual = registry.GetAll().Values.Where(x => x.AppControlState == LightControlState.Manual).ToArray();
            if (manual.Length == 0)
                logger.LogDebug("Manually controlled lights: none");
            else
            {
                logger.LogDebug("Manually controlled lights:");
                logger.LogLightProperties(manual);
            }
        }
    }
}
