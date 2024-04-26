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
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using TimeZoneConverter;

namespace HueShift2.Control
{
    public class AdaptiveScheduleProvider : IScheduleProvider
    {
        private readonly HueShiftMode mode;
        private readonly ILogger<AdaptiveScheduleProvider> logger;
        private readonly IConfiguration configuration; 
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;
        private readonly ILightColourCalculator lightColourCalculator;

        private AdaptiveSolarEvents solarEvents;

        public AdaptiveScheduleProvider(ILogger<AdaptiveScheduleProvider> logger, IConfiguration configuration, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, ILightColourCalculator lightColourCalculator)
        {
            this.mode = HueShiftMode.Adaptive;
            this.logger = logger;
            this.configuration = configuration;
            this.appOptionsDelegate = appOptionsDelegate;
            this.lightColourCalculator = lightColourCalculator;
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
                logger.LogError(e, $"HueShift2 does not support OSX");
                throw;
            }
        }

        private void RefreshSolarEvents(DateTime target)
        {
            var geolocation = appOptionsDelegate.CurrentValue.Geolocation;
            var tz = DetermineTimeZoneId(geolocation.TimeZone);

            var solarTimes = new SolarTimes(target, geolocation.Latitude, geolocation.Longitude);

            var sunrise = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunrise.ToUniversalTime(), tz);
            var sunset = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.Sunset.ToUniversalTime(), tz);
            var solar_noon = TimeZoneInfo.ConvertTimeFromUtc(solarTimes.SolarNoon.ToUniversalTime(), tz);
            
            var midnight = sunrise.Date;
            var adaptiveTransitionTimeLimits = appOptionsDelegate.CurrentValue.SolarTransitionTimeLimits;
            this.solarEvents = new AdaptiveSolarEvents(
                sunrise.Clamp(midnight + adaptiveTransitionTimeLimits.SunriseLower, midnight + adaptiveTransitionTimeLimits.SunriseUpper),
                solar_noon,
                sunset.Clamp(midnight + adaptiveTransitionTimeLimits.SunsetLower, midnight + adaptiveTransitionTimeLimits.SunsetUpper)
            ); 
            if (solarEvents.Sunrise.Date != solarEvents.Sunset.Date) throw new InvalidOperationException();
            logger.LogInformation($"Solar transition times refreshed | Day: {solarEvents.Sunrise.ToString(CultureInfo.InvariantCulture)} | Night: {solarEvents.Sunset.ToString(CultureInfo.InvariantCulture)}");
        }

        private bool RefreshRequired(DateTime currentTime, DateTime? lastRunTime)
        {
            if (lastRunTime == null) return true;
            if (this.solarEvents == null) return true;
            if ((currentTime.Date - solarEvents.Sunset.Date).TotalDays > 0) return true;
            if (solarEvents.Sunrise - currentTime < new TimeSpan(1, 0, 0) &&
                solarEvents.Sunrise - lastRunTime >= new TimeSpan(1, 0, 0))
            {
                return true;
            }
            return false;
        }

        public TransitionType TransitionRequired(DateTime currentTime, DateTime? lastRunTime, DateTime? lastTransitionTime)
        {
            if (RefreshRequired(currentTime, lastRunTime)) RefreshSolarEvents(currentTime);

            if (lastRunTime == null)
            {
                logger.LogInformation("Performing first run transition. Taking control of all non-excluded lights.");
                return TransitionType.FirstRun;
            }
            if (lastRunTime < solarEvents.Sunrise && currentTime >= solarEvents.Sunrise)
            {
                logger.LogInformation("Performing sunrise transition.");
                return TransitionType.Solar;
            }
            if (lastRunTime < solarEvents.Sunset && currentTime >= solarEvents.Sunset)
            {
                logger.LogInformation("Performing sunset transition.");
                return TransitionType.Solar;
            }
            if (lastRunTime < solarEvents.Sunset && currentTime >= solarEvents.Sunset)
            {
                logger.LogInformation("Performing sunset transition.");
                return TransitionType.Solar;
            }
            var transitionInterval = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.TransitionInterval);
            if (lastTransitionTime == null) return TransitionType.Null;
            if (currentTime - lastTransitionTime >= transitionInterval)
            {
                logger.LogInformation("Performing adaptive transition.");
                return TransitionType.Adaptive;
            }
            return TransitionType.Null;
        }

        public TimeSpan? GetTransitionDuration(TransitionType transitionType)
        {
            var options = appOptionsDelegate.CurrentValue;
            var transitionDurationSeconds = transitionType switch
            {
                TransitionType.FirstRun => options.BasicTransitionDuration,
                TransitionType.Adaptive => options.AdaptiveTransitionDuration,
                TransitionType.Solar => options.SolarTransitionDuration,
                _ => throw new NotImplementedException($"Invalid transition type: {transitionType}"),
            };
            return TimeSpan.FromSeconds(transitionDurationSeconds);
        }

        public bool IsReset(TransitionType transitionType)
        {
            return transitionType switch
            {
                TransitionType.FirstRun or TransitionType.Solar => true,
                TransitionType.Adaptive => false,
                _ => throw new NotImplementedException($"Invalid transition type: {transitionType}"),
            };
        }

        public bool IsSleep(DateTime currentTime)
        {
            var midnight = solarEvents.Sunrise.Date;
            var sleepDateTime = midnight + appOptionsDelegate.CurrentValue.Sleep;
            return currentTime >= sleepDateTime;
        }

        public AppLightState TargetLightState(DateTime currentTime)
        {
            var colourTemperatures = appOptionsDelegate.CurrentValue.ColourTemperature;
            var parameters = new AdaptiveCalculationParameters(
                solarEvents,
                colourTemperatures
            );
            var targetLightState = lightColourCalculator.SetBrightnessAndColour(parameters, currentTime, IsSleep(currentTime));
            logger.LogDebug($"Transition target lightstate: {targetLightState}");
            return targetLightState;
        }
    }
}
