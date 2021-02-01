using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class Transition
    {
        public readonly DateTime StartedTime;
        public readonly TimeSpan Duration;

        public Transition(DateTime currentTime, TimeSpan duration)
        {
            StartedTime = currentTime;
            Duration = duration;
        }

        public bool IsExpired(DateTime currentTime)
        {
            return currentTime - StartedTime >= Duration;
        }
    }
}
