using HueShift2.Interfaces;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HueShift2.Model
{
    public class Transition: IDeepCloneable<Transition>
    {
        public readonly DateTime StartedTime;
        public readonly TimeSpan Duration;

        public Transition(DateTime currentTime, TimeSpan duration)
        {
            StartedTime = currentTime;
            Duration = duration;
        }

        public Transition DeepClone()
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
