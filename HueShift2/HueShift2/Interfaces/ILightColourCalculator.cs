using HueShift2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Interfaces
{
    public interface ILightColourCalculator
    {
        public AppLightState SetBrightnessAndColour(LightCalculationParameters parameters, DateTime target, bool isSleep);
    }
}
