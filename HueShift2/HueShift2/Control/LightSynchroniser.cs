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
            var retrySyncCommands = new Dictionary<string, LightCommand>();
            foreach (var (id, light) in lights)
            {
                if (light.RequiresRetrySync(out var retryCommand))
                    retrySyncCommands.Add(id, retryCommand);
                else if (light.PowerState != LightPowerState.Syncing && light.PowerState != LightPowerState.Transitioning)
                {
                    if (light.RequiresSync(out var syncCommand))
                        syncCommands.Add(id, syncCommand);
                }
            }

            if (syncCommands.Any()) await Dispatch(syncCommands, lights, duration, currentTime, logSync: true);
            if (retrySyncCommands.Any()) await Dispatch(retrySyncCommands, lights, duration, currentTime, logSync: false);
        }

        private async Task Dispatch(IDictionary<string, LightCommand> commands, IReadOnlyDictionary<string, LightControlPair> lights, TimeSpan duration, DateTime currentTime, bool logSync)
        {
            var lightNames = commands.Keys.Select(id => lights[id].Properties.Name);
            foreach (var (id, command) in commands)
            {
                command.TransitionTime = duration;
                lights[id].ExecuteInstantaneousCommand(command);
                lights[id].ExecuteSync(duration, currentTime);
                await client.SendCommandAsync(command, new[] { id });
            }
            if (logSync)
            {
                var distinctTargets = commands.Keys.Select(id => lights[id].ExpectedLight).Distinct().ToList();
                var commonTarget = distinctTargets.Count == 1 ? distinctTargets[0] : null;
                logger.LogSync(lightNames, commonTarget);
            }
        }
    }
}
