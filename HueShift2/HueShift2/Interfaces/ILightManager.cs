using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightManager
    {
        public Task ExecuteRefresh(DateTime currentTime);
        public Task ExecuteTransitionCommand(LightState target, LightCommand command, DateTime currentTime, bool resumeControl);
        public Task OutputLightsOnNetwork(DateTime currentTime);
    }
}
