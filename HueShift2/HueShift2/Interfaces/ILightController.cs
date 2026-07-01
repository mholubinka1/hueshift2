using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightController
    {
        Task Refresh(DateTime currentTime);
        Task Transition(AppLightState target, LightCommand command, DateTime currentTime, bool resumeControl, TransitionType transitionType);
        void PrintAll();
        void PrintScheduled();
        void PrintManual();
    }
}
