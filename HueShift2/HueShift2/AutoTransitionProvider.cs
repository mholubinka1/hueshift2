using HueShift2.Configuration;
using HueShift2.Configuration.Model;
using HueShift2.Helpers;
using HueShift2.Interfaces;
using HueShift2.Model;
using Innovative.SolarCalculator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TimeZoneConverter;

namespace HueShift2
{
    public class AutoTransitionProvider : ITransitionProvider
    {
        private readonly HueShiftMode mode;
        private readonly ILogger<AutoTransitionProvider> logger;
        private readonly IConfiguration configuration; 
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        private AutoTransitionTimes transitionTimes;

        public AutoTransitionProvider(ILogger<AutoTransitionProvider> logger, IConfiguration configuration, IOptionsMonitor<HueShiftOptions> appOptionsDelegate)
        {
            this.mode = HueShiftMode.Auto;
            this.logger = logger;
            this.configuration = configuration;
            this.appOptionsDelegate = appOptionsDelegate;
        }

        public HueShiftMode Mode()
        {
            return mode;
        }

        private TimeZoneInfo DetermineTimeZoneId(string timeZone)
        {
            try
            {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                if (isWindows)
                {
                    var windowsId = TZConvert.IanaToWindows(timeZone);
                    return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                }
                var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
                if (isLinux)
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                }
                throw new PlatformNotSupportedException();
            }
            catch (Exception e)
            {
                logger.LogError(e, $"HueShift-2 does not support OSX");
                throw;
            }
        }

        private void RefreshTransitionTimes()
        {
            var geolocation = appOptionsDelegate.CurrentValue.Geolocation;
            var tz = DetermineTimeZoneId(geolocation.TimeZone);
            var solarTimes = new SolarTimes(DateTime.Now, geolocation.Latitude, geolocation.Longitude);
            var sunrise = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunrise.ToUniversalTime(), tz);
            var sunset = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunset.ToUniversalTime(), tz);
            var midnight = sunrise.Date;
            var autoTransitionTimeLimits = appOptionsDelegate.CurrentValue.AutoTransitionTimeLimits;
            this.transitionTimes = new AutoTransitionTimes(
                sunrise.Clamp(midnight + autoTransitionTimeLimits.SunriseLower, midnight + autoTransitionTimeLimits.SunriseUpper),
                sunset.Clamp(midnight + autoTransitionTimeLimits.SunsetLower, midnight + autoTransitionTimeLimits.SunsetUpper)
            );
        }

        public bool ShouldPerformTransition(DateTime currentTime, DateTime? lastRunTime)
        {
            RefreshTransitionTimes();
            if (lastRunTime == null) return true;            
            if (lastRunTime < transitionTimes.Day && currentTime >= transitionTimes.Day) return true;
            if (lastRunTime < transitionTimes.Night && currentTime >= transitionTimes.Night) return true;
            return false;
        }

        public TimeSpan? GetTransitionDuration(DateTime currentTime, DateTime? lastRunTime)
        {
            var options = appOptionsDelegate.CurrentValue;
            if (lastRunTime == null) return TimeSpan.FromSeconds(options.StandardTransitionTime);
            if (lastRunTime < transitionTimes.Day && currentTime >= transitionTimes.Day) return TimeSpan.FromSeconds(options.TransitionTimeAtSunriseAndSunset);
            if (lastRunTime < transitionTimes.Night && currentTime >= transitionTimes.Night) return TimeSpan.FromSeconds(options.TransitionTimeAtSunriseAndSunset);
            return TimeSpan.FromSeconds(options.StandardTransitionTime);
        }

        public bool IsReset(DateTime currentTime, DateTime? lastRunTime)
        {
            if (lastRunTime == null) return true;
            if (lastRunTime < transitionTimes.Day && currentTime >= transitionTimes.Day) return true;
            if (lastRunTime < transitionTimes.Night && currentTime >= transitionTimes.Night) return false;
            return false;
        }

        public LightState TargetLightState(DateTime currentTime)
        {
            var colourTemperatures = appOptionsDelegate.CurrentValue.ColourTemperature;
            var target = (currentTime <= transitionTimes.Day || currentTime >= transitionTimes.Night) 
                ? colourTemperatures.Night : colourTemperatures.Day;
            var colour =  new Colour(target);
            return new LightState(colour);
        }
    }
}
