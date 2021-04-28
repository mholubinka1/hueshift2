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
        public readonly LightPowerState PowerState;
        public readonly LightControlState AppControlState;
        public readonly AppLightState NetworkLight;
        public readonly AppLightState ExpectedLight;
        public readonly Transition Transition;
        public readonly bool ResetOccurred;

        public CachedControlPair(LightControlPair light)
        {
            this.Properties = light.Properties.DeepClone();
            this.PowerState = light.PowerState;
            this.AppControlState = light.AppControlState;
            this.NetworkLight = new AppLightState(light.NetworkLight);
            this.ExpectedLight = light.ExpectedLight.DeepClone();
            if (light.Transition != null)
            {
                this.Transition = light.Transition.DeepClone();
            }
            this.ResetOccurred = light.ResetOccurred;
        }

        public override string ToString()
        {
            var @base = $"Cached Pair | Id: {this.Properties.Id} Name: {this.Properties.Name} | {this.PowerState} - Control: {this.AppControlState}";
            if (this.PowerState == LightPowerState.Transitioning)
            {
                var remaining = ((this.Transition.StartedTime + this.Transition.Duration) - DateTime.Now).TotalSeconds;
                remaining = remaining < 0 ? 0 : remaining; 
                @base += $" | Transition Time Remaining: {remaining}s";
            }
            else
            {
                @base += "\n";
            }
            var networkLight = $" | Network Light | " + this.NetworkLight.ToString();
            var expectedLight = $" | Expected Light | " + this.ExpectedLight.ToString();
            return @base + networkLight + expectedLight;
        }
    }
}
