using HueShift2.Configuration;
using HueShift2.Interfaces;
using HueShift2.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2
{
    public class CustomTransitionProvider : ITransitionProvider
    {
        public HueShiftMode Mode()
        {
            throw new NotImplementedException();
        }

        public TimeSpan? GetTransitionDuration(DateTime currentTime, DateTime? lastRunTime)
        {
            throw new NotImplementedException();
        }

        public bool ShouldPerformTransition(DateTime currentTime, DateTime? lastRunTime)
        {
            throw new NotImplementedException();
        }

        public bool IsReset(DateTime currentTime, DateTime? lastRunTime)
        {
            throw new NotImplementedException();
        }

        public HueShiftLightState TargetLightState(DateTime currentTime)
        {
            throw new NotImplementedException();
        }
    }
}
