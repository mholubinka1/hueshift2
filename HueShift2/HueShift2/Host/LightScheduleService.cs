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
    public class LightScheduleService : BackgroundService
    {
        private readonly ILogger<LightScheduleService> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private readonly ILightManager lightManager;
        private readonly ILightScheduleWorker lightScheduler;

        public LightScheduleService(ILogger<LightScheduleService> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, ILightManager lightManager, ILightScheduleWorker lightScheduler)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.lightManager = lightManager;
            this.lightScheduler = lightScheduler;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var pollingFrequency = Math.Max(appOptionsDelegate.CurrentValue.PollingFrequency * 1000, appOptionsDelegate.CurrentValue.BasicTransitionDuration);
            await lightManager.Refresh(DateTime.Now);
            lightManager.PrintAll();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await lightScheduler.RunAsync();
                    await Task.Delay(pollingFrequency, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "RunAsync failed. Retrying...");
                }
            }
        }
    
    }
}