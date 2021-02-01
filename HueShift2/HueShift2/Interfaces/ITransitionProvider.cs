using HueShift2.Configuration;
using HueShift2.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Interfaces
{
    public interface ITransitionProvider
    {
        public HueShiftMode Mode();
        public bool ShouldPerformTransition(DateTime currentTime, DateTime? lastRunTime);
        public TimeSpan? GetTransitionDuration(DateTime currentTime, DateTime? lastRunTime);

        public bool IsReset(DateTime currentTime, DateTime? lastRunTime);
        public LightState TargetLightState(DateTime currentTime);
        
    }
}
