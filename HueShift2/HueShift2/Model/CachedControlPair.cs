using HueShift2.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Model
{
    public class CachedControlPair
    {
        public readonly LightProperties Properties;
        public readonly bool IsOn;
        public readonly bool IsTransitioning;
        public readonly bool IsReachable;
        public readonly LightControlState AppControlState;
        public readonly AppLightState NetworkLight;
        public readonly AppLightState ExpectedLight;
        public readonly double? TransitionSecondsRemaining;
        public readonly bool ResetOccurred;
        public readonly bool SyncRequired;

        public CachedControlPair(LightControlPair light)
        {
            this.Properties = light.Properties.DeepClone();
            this.IsOn = light.IsOn();
            this.IsTransitioning = light.IsTransitioning();
            this.IsReachable = light.IsReachable();
            this.AppControlState = light.AppControlState;
            this.NetworkLight = new AppLightState(light.NetworkLight);
            this.ExpectedLight = light.ExpectedLight.DeepClone();
            this.TransitionSecondsRemaining = light.TransitionSecondsRemaining();
            this.ResetOccurred = light.ResetOccurred;
            this.SyncRequired = light.SyncRequired;
        }

        public override string ToString()
        {
            var powerDesc = IsReachable ? (IsOn ? "On" : "Off") : "Unreachable";
            var @base = $"Cached Pair | Id: {this.Properties.Id} Name: {this.Properties.Name} | {powerDesc} - Control: {this.AppControlState}";
            if (IsTransitioning)
                @base += $" | Transition Time Remaining: {TransitionSecondsRemaining}s";
            var networkLight = $" | Network Light | " + this.NetworkLight.ToString();
            var expectedLight = $" | Expected Light | " + this.ExpectedLight.ToString();
            return @base + networkLight + expectedLight;
        }
    }
}
