using HueShift2.Configuration;
using HueShift2.Configuration.Model;
using HueShift2.Interfaces;
using HueShift2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;

namespace HueShift2.Control
{
    public class AdaptiveScheduleProvider : IScheduleProvider
    {
        private readonly HueShiftMode mode;
        private readonly ILogger<AdaptiveScheduleProvider> logger;
        private readonly IConfiguration configuration;
        private readonly IOptionsMonitor<HueShiftOptions> appOptionsDelegate;
        private readonly ILightColourCalculator lightColourCalculator;
        private readonly ISolarEventProvider solarEventProvider;

        private AdaptiveSolarEvents solarEvents;

        public AdaptiveScheduleProvider(ILogger<AdaptiveScheduleProvider> logger, IConfiguration configuration, IOptionsMonitor<HueShiftOptions> appOptionsDelegate, ILightColourCalculator lightColourCalculator, ISolarEventProvider solarEventProvider)
        {
            this.mode = HueShiftMode.Adaptive;
            this.logger = logger;
            this.configuration = configuration;
            this.appOptionsDelegate = appOptionsDelegate;
            this.lightColourCalculator = lightColourCalculator;
            this.solarEventProvider = solarEventProvider;
        }

        public HueShiftMode Mode()
        {
            return mode;
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
            if (RefreshRequired(currentTime, lastRunTime))
            {
                solarEvents = solarEventProvider.GetEventsForDate(DateOnly.FromDateTime(currentTime));
                logger.LogInformation($"Solar transition times refreshed | Day: {solarEvents.Sunrise.ToString(CultureInfo.InvariantCulture)} | Night: {solarEvents.Sunset.ToString(CultureInfo.InvariantCulture)}");
            }

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
            var transitionInterval = TimeSpan.FromSeconds(appOptionsDelegate.CurrentValue.TransitionInterval);
            if (lastTransitionTime == null) return TransitionType.Null;
            if (currentTime - lastTransitionTime >= transitionInterval)
            {
                return TransitionType.Adaptive;
            }
            return TransitionType.Null;
        }

        public TimeSpan? GetTransitionDuration(TransitionType transitionType)
        {
            var options = appOptionsDelegate.CurrentValue;
            var transitionDurationSeconds = transitionType switch
            {
                TransitionType.Sync => options.BasicTransitionDuration,
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
                TransitionType.Sync or TransitionType.Adaptive => false,
                _ => throw new NotImplementedException($"Invalid transition type: {transitionType}"),
            };
        }

        public bool IsSleep(DateTime currentTime)
        {
            if (solarEvents == null)
                throw new InvalidOperationException("Solar events have not been loaded. Call TransitionRequired before IsSleep.");
            var midnight = solarEvents.Sunrise.Date;
            var sleepDateTime = midnight + appOptionsDelegate.CurrentValue.Sleep;
            if (currentTime >= sleepDateTime) return true;
            if (currentTime <= solarEvents.Sunrise) return true;
            return false;
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
