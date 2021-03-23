using HueShift2.Helpers;
using HueShift2.Model;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class LightControlPair
    {
        public LightPowerState PowerState { get; private set; }
        public LightControlState AppControlState { get; private set; }
        public Light NetworkLight { get; private set; }
        public AppLight ExpectedLight { get; private set; }

        public AppLight CachedLight { get; private set; }

        public Transition Transition;

        public LightControlPair(Light networkLight)
        {
            
        }

        public void RefreshTransition(bool isOn, DateTime currentTime)
        {
            if (isOn)
            {
                if (this.PowerState == LightPowerState.Transitioning)
                {
                    if (this.Transition == null) throw new NullReferenceException();
                    if (this.Transition.IsExpired(currentTime))
                    {
                        this.PowerState = LightPowerState.On;
                        this.Transition = null;
                    }
                }

            }
            else
            {
                this.PowerState = LightPowerState.Off;
                this.Transition = null;
            }
        }

        public void Refresh(Light networkLight, DateTime currentTime)
        {
            this.NetworkLight = networkLight;
            var isOn = NetworkLight.State.On;
            RefreshTransition(isOn, currentTime);
            if (isOn)
            {
                //TODO: 1. turning lights "to expected on state" turns brightness up to 100%
                //TODO: 2. turning on the lights "to a new state - different brightness and/or colour state" sets lights to Hybrid Manual
                //TODO: maybe separate these with switch
                if (this.AppControlState == LightControlState.HueShift)
                {
                    if (this.NetworkLight.LightEquals(this.ExpectedLight) && this.PowerState != LightPowerState.Transitioning)
                    {
                        this.AppControlState = LightControlState.Manual;
                    }
                }
                if(this.AppControlState == LightControlState.Manual)
                {
                    if (this.PowerState == LightPowerState.Off)
                    {
                        this.AppControlState = LightControlState.HueShift;
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
