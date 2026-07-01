using HueShift2.Configuration.Model;
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
    public class LightSynchroniser : ILightSynchroniser
    {
        private readonly ILogger<LightSynchroniser> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;
        private readonly ILocalHueClient client;

        public LightSynchroniser(ILogger<LightSynchroniser> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, ILocalHueClient client)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.client = client;
        }

        public async Task Synchronise(IReadOnlyDictionary<string, LightControlPair> lights, DateTime currentTime)
        {
            var duration = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.BasicTransitionDuration);

            var syncCommands = new Dictionary<string, LightCommand>();
            foreach (var (id, light) in lights)
            {
                if (light.PowerState != LightPowerState.Transitioning)
                {
                    if (light.RequiresSync(out var syncCommand))
                        syncCommands.Add(id, syncCommand);
                }
            }

            if (syncCommands.Any()) await Dispatch(syncCommands, lights, duration, currentTime);
        }

        private async Task Dispatch(IDictionary<string, LightCommand> commands, IReadOnlyDictionary<string, LightControlPair> lights, TimeSpan duration, DateTime currentTime)
        {
            foreach (var (id, command) in commands)
            {
                command.TransitionTime = duration;
                lights[id].ExecuteCommand(command, currentTime, TransitionType.Adaptive);
                await client.SendCommandAsync(command, new[] { id });
            }
            var lightNames = commands.Keys.Select(id => lights[id].Properties.Name);
            logger.LogSync(lightNames, lights[commands.Keys.First()].ExpectedLight);
        }
    }
}
