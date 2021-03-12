using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightMemoryPair
    {
        public LightPowerState PowerState { get; private set; }
        public LightControlState AppControlState { get; private set; }
        public Light NetworkLight { get; private set; }
        public AppLight CachedLight { get; private set; }

        public LightMemoryPair(Light networkLight)
        {
            
        }

        public void Refresh(Light networkLight, DateTime currentTime)
        {
            this.NetworkLight = networkLight;
            var on = NetworkLight.State.On;






            //TODO: 1. turning lights "to expected on state" turns brightness up to 100%
            //TODO: 2. turning on the lights "to a new state - different brightness and/or colour state" sets lights to Hybrid Manual
            if (on)
            {
                if (this.AppControlState == LightControlState.HueShift)
                {
                    if (!this.State.Matches(light.State) && this.State.PowerState != LightPowerState.Transitioning)
                    {
                        this.ControlState = LightControlState.Manual;
                    }
                }
                if (this.ControlState == LightControlState.Manual)
                {
                    if (this.State.PowerState == LightPowerState.Off)
                    {
                        this.ControlState = LightControlState.HueShift;
                    }
                }
            }
            this.State.Refresh(currentTime, on);
        }

        public bool RequiresSync(out LightCommand syncCommand)
        {
            syncCommand = null;
            return false;
        }

        public void Exclude()
        {
            AppControlState = LightControlState.Excluded;
        }
    }
}
