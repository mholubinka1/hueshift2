using HueShift2.Configuration.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Model
{
    public class LightCalculationParameters
    {

    }

    public class AdaptiveCalculationParameters : LightCalculationParameters
    {
        public AdaptiveSolarEvents SolarEvents {get; private set; }

        public AdaptiveCalculationParameters(AdaptiveSolarEvents solarEvents, ColourTemperature colourTemperature)
        {
            this.SolarEvents = solarEvents;
        }
    }
}
