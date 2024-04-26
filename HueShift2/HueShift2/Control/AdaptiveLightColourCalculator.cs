using HueShift2.Configuration.Model;
using HueShift2.Interfaces;
using HueShift2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HueShift2.Control
{
    public class AdaptiveLightColourCalculator : ILightColourCalculator
    {
        private ILogger<LightManager> logger;
        private IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        public AdaptiveLightColourCalculator(ILogger<LightManager> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
        }

        private byte? CalculateBrightnessPct(bool isSleep)
        {
            if (isSleep) {
                var percentage = appOptionsDelegate.CurrentValue.NightBrightnessPercentage;
                return (byte)(percentage / 100 * Byte.MaxValue);
            }
            return Byte.MaxValue;
        }

        private int CalculateKelvinColourTemperature(double sunPosition)
        {
            var warmest = appOptionsDelegate.CurrentValue.ColourTemperature.Warmest;
            var coolest = appOptionsDelegate.CurrentValue.ColourTemperature.Coolest;
            if (sunPosition > 0) {
                var range = warmest - coolest;
                var colourTemperature = (int)(warmest - (range * sunPosition));
                if (colourTemperature > warmest) return warmest;
                if (colourTemperature < coolest) return coolest;
                return colourTemperature;
            }
            return warmest;
        }

        private static double CalculateSunPosition(AdaptiveSolarEvents events, DateTime currentTime)
        {
            if (currentTime < events.Sunset && currentTime > events.Sunrise)
            {
                if (currentTime < events.SolarNoon)
                {
                    var scalingFactor = 1.0 - Math.Pow(((currentTime.TimeOfDay - events.SolarNoon.TimeOfDay) / (events.SolarNoon.TimeOfDay - events.Sunrise.TimeOfDay)), 2.0);
                    return 1.0 * scalingFactor;
;                }
                if (currentTime > events.SolarNoon)
                {
                    var scalingFactor = 1.0 - Math.Pow(((currentTime.TimeOfDay - events.SolarNoon.TimeOfDay) / (events.SolarNoon.TimeOfDay - events.Sunset.TimeOfDay)), 2.0);
                    return 1.0 * scalingFactor;
                }
            }
            if ((currentTime < events.Sunrise && currentTime > currentTime.Date) || currentTime > events.Sunset) return -1.0;
            throw new InvalidOperationException();
        }

        public AppLightState SetBrightnessAndColour(LightCalculationParameters lightCalculationParameters, DateTime currentTime, bool isSleep)
        {
            AdaptiveCalculationParameters adaptiveParameters = (AdaptiveCalculationParameters)lightCalculationParameters;
            var brightness = CalculateBrightnessPct(isSleep);
            var sunPosition = CalculateSunPosition(adaptiveParameters.SolarEvents, currentTime);
            var colourTemperature = CalculateKelvinColourTemperature(sunPosition);
            return new AppLightState(brightness, colourTemperature);
        }
    }
}