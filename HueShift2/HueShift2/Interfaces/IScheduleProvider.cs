using HueShift2.Configuration;
using HueShift2.Model;
using System;

namespace HueShift2.Interfaces
{
    public interface IScheduleProvider
    {
        public HueShiftMode Mode();
        public TransitionType TransitionRequired(DateTime currentTime, DateTime? lastRunTime, DateTime? lastTransitionTime);
        public TimeSpan? GetTransitionDuration(TransitionType transitionType);
        public bool IsReset(TransitionType transitionType);
        public AppLightState TargetLightState(DateTime currentTime);       
    }
}
