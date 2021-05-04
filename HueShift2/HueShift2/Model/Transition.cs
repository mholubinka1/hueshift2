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
        public double SecondsRemaining { get; private set; }

        public Transition(DateTime currentTime, TimeSpan duration)
        {
            StartedTime = currentTime;
            Duration = duration;
            SecondsRemaining = duration.TotalSeconds;
        }

        public Transition(DateTime startedTime, TimeSpan duration, double secondsRemaining)
        {
            StartedTime = startedTime;
            Duration = duration;
            SecondsRemaining = secondsRemaining;
        }

        public Transition DeepClone()
        {
            return new Transition(this.StartedTime, this.Duration, this.SecondsRemaining);
        }

        public bool IsExpired(DateTime currentTime)
        {
            var expired = currentTime - StartedTime >= Duration;
            this.SecondsRemaining = expired ? 0 : ((this.StartedTime + this.Duration) - currentTime).TotalSeconds;
            return expired;
        }

        public override string ToString()
        {
            return $"Started at: {this.StartedTime.ToString(CultureInfo.InvariantCulture)} Duration: {this.Duration.TotalSeconds}";
        }

    }
}
