using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public Transition Clone()
        {
            return new Transition(this.StartedTime, this.Duration);
        }

        public bool IsExpired(DateTime currentTime)
        {
            return currentTime - StartedTime >= Duration;
        }

        public override string ToString()
        {
            return $"Started at: {this.StartedTime.ToString(CultureInfo.InvariantCulture)} Duration: {this.Duration.TotalSeconds}";
        }
    }
}
