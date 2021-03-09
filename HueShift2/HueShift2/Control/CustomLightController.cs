using HueShift2.Configuration;
using HueShift2.Configuration.Model;
using HueShift2.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class CustomLightController : ILightController
    {
        private readonly HueShiftMode mode;
        private readonly ILogger<CustomLightController> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private IHueClientManager clientManager;
        private ILocalHueClient client;

        public CustomLightController(ILogger<CustomLightController> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, IOptionsMonitor<CustomScheduleOptions> scheduleOptionsDelegate,
            IHueClientManager clientManager, ILocalHueClient client)
        {
            this.mode = HueShiftMode.Auto;
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
            this.clientManager = clientManager;
            this.client = client;
        }

        public async Task<DateTime?> Execute(DateTime? lastRunTime)
        {
            throw new NotImplementedException();
        }

        public HueShiftMode Mode()
        {
            return mode;
        }
    }
}
