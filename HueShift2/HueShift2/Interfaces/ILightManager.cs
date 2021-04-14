using HueShift2.Control;
using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightManager
    {
        public Task Refresh(DateTime currentTime);
        public Task Transition(AppLightState target, LightCommand command, DateTime currentTime, bool resumeControl);
        public void PrintAll();
        public void PrintScheduled();
        public void PrintManual();
        public void PrintExcluded();

    }
}
