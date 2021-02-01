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

namespace HueShift2
{
    public class LightScheduler : ILightScheduler
    {
        private readonly ILogger<LightScheduler> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private readonly IEnumerable<ILightController> lightControllers;

        private DateTime? lastRunTime;

        public LightScheduler(ILogger<LightScheduler> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate,
            IEnumerable<ILightController> lightControllers)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.lightControllers = lightControllers;
        }

        private ILightController ResolveController()
        {
            var controller = lightControllers.First(x => x.Mode() == appOptionsDelegate.CurrentValue.Mode);
            return controller;
        }

        public async Task RunAsync()
        {
            var controller = ResolveController();
            var currentTime = await controller.Execute(lastRunTime);
            lastRunTime = currentTime;
        }
    }
}
