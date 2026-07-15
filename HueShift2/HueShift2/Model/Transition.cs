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
        public readonly TransitionType Kind;
        public double SecondsRemaining { get; private set; }

        public Transition(DateTime currentTime, TimeSpan duration, TransitionType kind)
        {
            StartedTime = currentTime;
            Duration = duration;
            Kind = kind;
            SecondsRemaining = duration.TotalSeconds;
        }

        public Transition(DateTime startedTime, TimeSpan duration, double secondsRemaining, TransitionType kind)
        {
            StartedTime = startedTime;
            Duration = duration;
            Kind = kind;
            SecondsRemaining = secondsRemaining;
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
