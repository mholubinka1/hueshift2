using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public class AutoTransitionTimes
    {
        public readonly DateTime Day;
        public readonly DateTime Night;

        public AutoTransitionTimes(DateTime day, DateTime night)
        {
            Day = day;
            Night = night;
        }
    }
}
