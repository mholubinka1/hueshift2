using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Configuration.Model
{
    public class LightingConfiguration
    {
        public readonly HueShiftOptions HueShiftOptions;

        public LightingConfiguration(HueShiftOptions hueShiftOptions)
        {
            HueShiftOptions = hueShiftOptions;
        }
    }
}