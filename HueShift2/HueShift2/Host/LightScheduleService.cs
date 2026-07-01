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

        private readonly ILightController lightController;
        private readonly ILightScheduleWorker lightScheduler;

        public LightScheduleService(ILogger<LightScheduleService> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, ILightController lightController, ILightScheduleWorker lightScheduler)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.lightController = lightController;
            this.lightScheduler = lightScheduler;
        }

        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var pollingFrequency = Math.Max(appOptionsDelegate.CurrentValue.PollingFrequency, appOptionsDelegate.CurrentValue.BasicTransitionDuration) * 1000;
            await lightController.Refresh(DateTime.Now);
            lightController.PrintAll();
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