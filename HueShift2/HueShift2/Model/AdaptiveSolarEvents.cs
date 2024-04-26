using System;
using System.Globalization;

namespace HueShift2.Model
{
    public class AdaptiveSolarEvents
    {
        public readonly DateTime Sunrise;
        public readonly DateTime SolarNoon;
        public readonly DateTime Sunset;

        public AdaptiveSolarEvents(DateTime sunrise, DateTime solarNoon, DateTime sunset)
        {
            this.Sunrise = sunrise;
            this.SolarNoon = solarNoon;
            this.Sunset = sunset;
            if (!ValidateOrder()) 
            {
                throw new InvalidOperationException($"Solar Event times in the wrong order | Sunrise: {Sunrise.ToString(CultureInfo.InvariantCulture)} | Solar Noon: {SolarNoon.ToString(CultureInfo.InvariantCulture)} | Sunset: {Sunset.ToString(CultureInfo.InvariantCulture)}");
            }
        }

        private bool ValidateOrder()
        {
            if (Sunrise < SolarNoon && SolarNoon < Sunset) return true;
            return false;
        }
    }
}
