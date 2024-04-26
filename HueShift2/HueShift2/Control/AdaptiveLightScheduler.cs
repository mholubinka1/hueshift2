using HueShift2.Configuration;
using HueShift2.Configuration.Model;
using HueShift2.Interfaces;
using HueShift2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class AdaptiveLightScheduler: ILightScheduler
    {
        private readonly HueShiftMode mode;
        private readonly ILogger<AdaptiveLightScheduler> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private IScheduleProvider scheduleProvider;
        private ILightManager lightManager;
   
        public AdaptiveLightScheduler(ILogger<AdaptiveLightScheduler> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate,
            ILightManager lightManager, IEnumerable<IScheduleProvider> scheduleProviders)
        {
            this.mode = HueShiftMode.Adaptive;
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.scheduleProvider = scheduleProviders.First(x => x.Mode() == this.mode);
            this.lightManager = lightManager;
        }

        public HueShiftMode Mode()
        {
            return mode;
        }

        private static LightCommand CreateCommand(AppLightState light, TimeSpan? duration)
        {
            var command = new LightCommand
            {
                Brightness = light.Brightness,
                ColorTemperature = light.Colour.ColourTemperature,
                TransitionTime = duration
            };
            return command;
        }

        private async Task ExecuteTransition(DateTime currentTime, DateTime? lastRunTime, TransitionType transitionType)
        { 
            var transitionDuration = scheduleProvider.GetTransitionDuration(transitionType);
            var reset = scheduleProvider.IsReset(transitionType);
            var targetLightState = scheduleProvider.TargetLightState(currentTime);
            var command = CreateCommand(targetLightState, transitionDuration);
            await lightManager.Transition(targetLightState, command, currentTime, reset);
        }

        public async Task<(bool, DateTime?)> Execute(DateTime? lastRunTime, DateTime? lastTransitionTime)
        {
            logger.LogDebug("Executing automatic light control...");
            var currentTime = DateTime.Now;
            var transition = scheduleProvider.TransitionRequired(currentTime, lastRunTime, lastTransitionTime);
            if (transition == TransitionType.Null)
            {
                logger.LogDebug("No transition to perform.");
                await lightManager.Refresh(currentTime);
                return (false, currentTime);
            }
            await ExecuteTransition(currentTime, lastRunTime, transition);
            return (true, currentTime);
        }
    }
}
