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
        public int StandardTransitionTime { get; set; }
        public int TransitionTimeAtSunriseAndSunset { get; set; }
        public AutoTransitionTimeLimits AutoTransitionTimeLimits {get; set;}
        public string[] LightsToExclude { get; set; } = Array.Empty<string>();
        public BridgeProperties BridgeProperties { get; set; }
        public Geolocation Geolocation { get; set; }
        public ColourTemperature ColourTemperature { get; set; }

        public HueShiftOptions()
        {

        }

        public void SetDefaults()
        {
            Mode = HueShiftMode.Auto;
            StandardTransitionTime = 5;
            TransitionTimeAtSunriseAndSunset = 120;
            AutoTransitionTimeLimits = new AutoTransitionTimeLimits
            {
                SunriseLower = new TimeSpan(6, 0, 0),
                SunriseUpper = new TimeSpan(8, 30, 0),
                SunsetLower = new TimeSpan(18, 0, 0),
                SunsetUpper = new TimeSpan(20, 30, 0),
            };
            PollingFrequency = 10;
            LightsToExclude = Array.Empty<string>();
            ColourTemperature = new ColourTemperature
            {
                Day = 250,
                Night = 454,
            };
            return;
        }
    }
}
