using HueShift2.Configuration;
using HueShift2.Configuration.Model;
using HueShift2.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HueShift2.Host
{
    public class LightScheduleWorker : ILightScheduleWorker
    {
        private readonly ILogger<LightScheduleWorker> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private readonly IEnumerable<ILightScheduler> lightSchedulers;

        private DateTime? lastRunTime;
        private DateTime? lastTransitionTime;

        public LightScheduleWorker(ILogger<LightScheduleWorker> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate,
            IEnumerable<ILightScheduler> lightSchedulers)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.lightSchedulers = lightSchedulers;
        }

        private ILightScheduler ResolveScheduler()
        {
            var controller = lightSchedulers.First(x => x.Mode() == appOptionsDelegate.CurrentValue.Mode);
            return controller;
        }

        public async Task RunAsync()
        {
            var scheduler = ResolveScheduler();
            (bool transitionOccurred, DateTime? currentTime) = await scheduler.Execute(lastRunTime, lastTransitionTime);
            lastRunTime = currentTime;
            if (transitionOccurred) lastTransitionTime = currentTime;
        }
    }
}
