using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Model
{
    public enum LightPowerState
    {
        On,
        Off,
        Transitioning,
        Syncing,
    }

    public static class HelperMethods
    {
        public static LightPowerState ToPowerState(this bool on)
        {
            return on ? LightPowerState.On : LightPowerState.Off;
        }
    }
}
