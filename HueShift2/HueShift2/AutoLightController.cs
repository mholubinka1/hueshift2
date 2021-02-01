using HueShift2.Configuration;
using HueShift2.Configuration.Model;
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
    public class AutoLightController: ILightController
    {
        private readonly HueShiftMode mode;
        private readonly ILogger<AutoLightController> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private ITransitionProvider transitionProvider;
        private ILightManager lightManager;
   
        public AutoLightController(ILogger<AutoLightController> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, IOptionsMonitor<CustomScheduleOptions> scheduleOptionsDelegate,
            ILightManager lightManager, IEnumerable<ITransitionProvider> transitionProviders)
        {
            this.mode = HueShiftMode.Auto;
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.transitionProvider = transitionProviders.First(x => x.Mode() == this.mode);
            this.lightManager = lightManager;
        }

        public HueShiftMode Mode()
        {
            return mode;
        }

        private async Task PerformTransition(DateTime currentTime, DateTime? lastRunTime)
        {
            logger.LogInformation("");
            var transitionDuration = transitionProvider.GetTransitionDuration(currentTime, lastRunTime);
            var resumeControl = transitionProvider.IsReset(currentTime, lastRunTime);
            var targetColourTemperature = transitionProvider.TargetLightState(currentTime).Colour.ColourTemperature;
            var command = new LightCommand
            {
                ColorTemperature = targetColourTemperature,
                TransitionTime = transitionDuration
            };
            await lightManager.ExecuteTransitionCommand(command, currentTime, resumeControl);
        }

        public async Task<DateTime?> Execute(DateTime? lastRunTime)
        {
            logger.LogInformation("Executing automatic light control...");
            var currentTime = DateTime.Now;
            if (!transitionProvider.ShouldPerformTransition(currentTime, lastRunTime))
            {
                logger.LogInformation("Refreshing...");
                await lightManager.ExecuteRefresh(currentTime);
                return currentTime;
            }
            await PerformTransition(currentTime, lastRunTime);
            return currentTime;
        }
    }
}
