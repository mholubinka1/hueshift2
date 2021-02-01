using HueShift2.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace HueShift2.Configuration.Model
{
    public class CustomTransition
    {
        public DateTime TransitionTime;
        public TimeSpan Duration;
        public string Scene;
        public double[] ColourCoordinates;
        public int ColourTemperature;
        public int[] RGB;
        public string Hex;
    }
}
