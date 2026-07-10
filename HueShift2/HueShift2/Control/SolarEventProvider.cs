using HueShift2.Configuration.Model;
using HueShift2.Helpers;
using HueShift2.Interfaces;
using HueShift2.Model;
using Innovative.SolarCalculator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Runtime.InteropServices;
using TimeZoneConverter;

namespace HueShift2.Control
{
    public class SolarEventProvider : ISolarEventProvider
    {
        private readonly ILogger<SolarEventProvider> logger;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;

        public SolarEventProvider(ILogger<SolarEventProvider> logger, IOptionsMonitor<HueShiftOptions> appOptionsDelegate)
        {
            this.logger = logger;
            this.appOptionsDelegate = appOptionsDelegate;
        }

        private TimeZoneInfo ResolveTimeZone(string ianaTimeZone)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var windowsId = TZConvert.IanaToWindows(ianaTimeZone);
                    return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZone);

                throw new PlatformNotSupportedException();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to resolve timezone {TimeZone}", ianaTimeZone);
                throw;
            }
        }

        public AdaptiveSolarEvents GetEventsForDate(DateOnly date)
        {
            var options = appOptionsDelegate.CurrentValue;
            var tz = ResolveTimeZone(options.Geolocation.TimeZone);

            var target = date.ToDateTime(TimeOnly.MinValue);
            var solarTimes = new SolarTimes(target, options.Geolocation.Latitude, options.Geolocation.Longitude);

            var sunrise = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunrise.ToUniversalTime(), tz);
            var solarNoon = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.SolarNoon.ToUniversalTime(), tz);
            var sunset = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunset.ToUniversalTime(), tz);

            var midnight = sunrise.Date;
            var limits = options.SolarTransitionTimeLimits;

            var events = new AdaptiveSolarEvents(
                sunrise.Clamp(midnight + limits.SunriseLower, midnight + limits.SunriseUpper),
                solarNoon,
                sunset.Clamp(midnight + limits.SunsetLower, midnight + limits.SunsetUpper)
            );

            if (events.Sunrise.Date != events.Sunset.Date)
                throw new InvalidOperationException(
                    $"Sunrise and Sunset fall on different dates after clamping: {events.Sunrise:O} / {events.Sunset:O}");

            return events;
        }
    }
}
