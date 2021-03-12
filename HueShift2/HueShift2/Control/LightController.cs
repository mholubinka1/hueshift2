using HueShift2.Interfaces;
using Microsoft.Extensions.Logging;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightController : ILightController
    {
        private ILogger<LightController> logger;

        private ILocalHueClient client;

        public LightController(ILogger<LightController> logger, ILocalHueClient client)
        {

        }

        public async Task ExecuteTransitionCommand()
        {

        }

        public async Task ExecuteSynchronisationCommand()
        {

        }
    }
}
