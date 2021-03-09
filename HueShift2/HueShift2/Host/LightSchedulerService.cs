using HueShift2.Configuration.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HueShift2.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace HueShift2
{
    public class LightSchedulerService : BackgroundService
    {
        private readonly ILogger<LightSchedulerService> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private readonly ILightManager lightManager;
        private readonly ILightScheduler lightScheduler;

        public LightSchedulerService(ILogger<LightSchedulerService> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, ILightManager lightManager, ILightScheduler lightScheduler)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.lightManager = lightManager;
            this.lightScheduler = lightScheduler;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var pollingFrequency = Math.Max(appOptionsDelegate.CurrentValue.PollingFrequency * 1000, appOptionsDelegate.CurrentValue.StandardTransitionTime);
            await lightManager.OutputLightsOnNetwork(DateTime.Now);
            while (!cancellationToken.IsCancellationRequested)
            {
                await lightScheduler.RunAsync();
                await Task.Delay(pollingFrequency, cancellationToken);
            }
        }
    
    }
}