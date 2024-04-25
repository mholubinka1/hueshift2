using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace HueShift2.Configuration.Model
{
    public class HueShiftOptions
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public HueShiftMode Mode { get; set; }

        public int PollingFrequency { get; set; }
        public int TransitionInterval { get; set; }
        public int BasicTransitionDuration { get; set; }
        public int AdaptiveTransitionDuration { get; set; }
        public int SolarTransitionDuration { get; set; }
        public SolarTransitionTimeLimits SolarTransitionTimeLimits { get; set; }
        public TimeSpan Sleep { get; set; }
        public string[] LightsToExclude { get; set; } = Array.Empty<string>();
        public BridgeProperties BridgeProperties { get; set; }
        public Geolocation Geolocation { get; set; }
        public ColourTemperature ColourTemperature { get; set; }

        public int NightBrightnessPercentage {get; set; }

        public HueShiftOptions()
        {

        }

        public void SetDefaults()
        {
            Mode = HueShiftMode.Adaptive;
            BasicTransitionDuration = 5;
            AdaptiveTransitionDuration = 30;
            SolarTransitionDuration = 120;
            SolarTransitionTimeLimits = new SolarTransitionTimeLimits
            {
                SunriseLower = new TimeSpan(6, 0, 0),
                SunriseUpper = new TimeSpan(8, 0, 0),
                SunsetLower = new TimeSpan(18, 0, 0),
                SunsetUpper = new TimeSpan(20, 0, 0),
            };
            Sleep = new TimeSpan(23, 0, 0);
            PollingFrequency = 10;
            TransitionInterval = 600;
            LightsToExclude = Array.Empty<string>();
            ColourTemperature = new ColourTemperature
            {
                Coolest = 250,
                Warmest = 454,
            };
            NightBrightnessPercentage = 60;
            return;
        }
    }
}
