using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Configuration.Model
{
    public class SolarTransitionTimeLimits
    {
        public TimeSpan SunriseLower { get; set; } = new TimeSpan(6, 0, 0);
        public TimeSpan SunriseUpper { get; set; } = new TimeSpan(8, 30, 0);
        public TimeSpan SunsetLower { get; set; } = new TimeSpan(18, 0, 0);
        public TimeSpan SunsetUpper { get; set; } = new TimeSpan(20, 30, 0);

        public SolarTransitionTimeLimits()
        {

        }
    }
}
