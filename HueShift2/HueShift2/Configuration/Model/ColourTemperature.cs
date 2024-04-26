using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Configuration.Model
{
    public class ColourTemperature
    {
        public int Coolest { get; set; } = 250;
        public int Warmest { get; set; } = 454;

        public ColourTemperature()
        {

        }
    }
}
