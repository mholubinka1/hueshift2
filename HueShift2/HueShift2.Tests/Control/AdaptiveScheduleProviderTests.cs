using HueShift2.Configuration.Model;
using HueShift2.Control;
using HueShift2.Interfaces;
using HueShift2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using Xunit;

namespace HueShift2.Tests.Control
{
    public class AdaptiveScheduleProviderTests
    {
        private static readonly DateTime Today = new DateTime(2024, 6, 21);
        private static readonly AdaptiveSolarEvents FixedEvents = new AdaptiveSolarEvents(
            Today.AddHours(7),
            Today.AddHours(12),
            Today.AddHours(20));

        private static AdaptiveScheduleProvider BuildProvider(
            ISolarEventProvider solarEventProvider,
            HueShiftOptions? options = null)
        {
            options ??= DefaultOptions();
            var monitor = Substitute.For<IOptionsMonitor<HueShiftOptions>>();
            monitor.CurrentValue.Returns(options);
            var config = Substitute.For<IConfiguration>();
            var calculator = Substitute.For<ILightColourCalculator>();
            return new AdaptiveScheduleProvider(
                NullLogger<AdaptiveScheduleProvider>.Instance,
                config,
                monitor,
                calculator,
                solarEventProvider);
        }

        private static AdaptiveScheduleProvider BuildInitialisedProvider(
            ISolarEventProvider solarEventProvider,
            HueShiftOptions? options = null)
        {
            var provider = BuildProvider(solarEventProvider, options);
            provider.TransitionRequired(Today.AddHours(8), null, null);
            return provider;
        }

        private static HueShiftOptions DefaultOptions() => new HueShiftOptions
        {
            TransitionInterval = 60,
            Sleep = new TimeSpan(23, 0, 0),
        };

        private static ISolarEventProvider FakeSolarProvider()
        {
            var provider = Substitute.For<ISolarEventProvider>();
            provider.GetEventsForDate(Arg.Any<DateOnly>()).Returns(FixedEvents);
            return provider;
        }

        [Fact]
        public void TransitionRequired_ReturnsFirstRun_WhenLastRunTimeIsNull()
        {
            // Given: fixed solar events, no prior run
            var scheduleProvider = BuildProvider(FakeSolarProvider());

            // When
            var result = scheduleProvider.TransitionRequired(Today.AddHours(8), null, null);

            // Then
            Assert.Equal(TransitionType.FirstRun, result);
        }

        [Fact]
        public void TransitionRequired_ReturnsSolar_WhenCurrentTimeCrossesSunrise()
        {
            // Given: fixed sunrise at 07:00, lastRunTime just before, currentTime just after
            var scheduleProvider = BuildInitialisedProvider(FakeSolarProvider());

            // When
            var result = scheduleProvider.TransitionRequired(
                Today.AddHours(7).AddMinutes(1),
                Today.AddHours(6).AddMinutes(59),
                Today.AddHours(6).AddMinutes(59));

            // Then
            Assert.Equal(TransitionType.Solar, result);
        }

        [Fact]
        public void TransitionRequired_ReturnsSolar_WhenCurrentTimeCrossesSunset()
        {
            // Given: fixed sunset at 20:00, lastRunTime just before, currentTime just after
            var scheduleProvider = BuildInitialisedProvider(FakeSolarProvider());

            // When
            var result = scheduleProvider.TransitionRequired(
                Today.AddHours(20).AddMinutes(1),
                Today.AddHours(19).AddMinutes(59),
                Today.AddHours(19).AddMinutes(59));

            // Then
            Assert.Equal(TransitionType.Solar, result);
        }

        [Fact]
        public void TransitionRequired_ReturnsAdaptive_WhenTransitionIntervalElapsed()
        {
            // Given: transition interval = 60s, last transition 61s ago
            var scheduleProvider = BuildInitialisedProvider(FakeSolarProvider());

            var currentTime = Today.AddHours(10);
            var lastRunTime = currentTime.AddSeconds(-61);
            var lastTransitionTime = currentTime.AddSeconds(-61);

            // When
            var result = scheduleProvider.TransitionRequired(currentTime, lastRunTime, lastTransitionTime);

            // Then
            Assert.Equal(TransitionType.Adaptive, result);
        }

        [Fact]
        public void TransitionRequired_ReturnsNull_WhenTransitionIntervalNotYetElapsed()
        {
            // Given: transition interval = 60s, last transition only 30s ago
            var scheduleProvider = BuildInitialisedProvider(FakeSolarProvider());

            var currentTime = Today.AddHours(10);
            var lastRunTime = currentTime.AddSeconds(-30);
            var lastTransitionTime = currentTime.AddSeconds(-30);

            // When
            var result = scheduleProvider.TransitionRequired(currentTime, lastRunTime, lastTransitionTime);

            // Then
            Assert.Equal(TransitionType.Null, result);
        }

        [Fact]
        public void SolarProvider_IsCalledOnce_WhenQueriedMultipleTimesSameDay()
        {
            // Given: provider initialised (1 solar fetch already made)
            var fakeSolar = FakeSolarProvider();
            var scheduleProvider = BuildInitialisedProvider(fakeSolar);

            // When: second call same day
            scheduleProvider.TransitionRequired(
                Today.AddHours(10),
                Today.AddHours(8),
                Today.AddHours(8));

            // Then: ISolarEventProvider was called exactly once for today's date (no re-fetch same day)
            fakeSolar.Received(1).GetEventsForDate(DateOnly.FromDateTime(Today));
        }

        [Fact]
        public void SolarProvider_IsCalledAgain_WhenDateChanges()
        {
            // Given: provider initialised on day 1 (1 solar fetch already made)
            var fakeSolar = FakeSolarProvider();
            var scheduleProvider = BuildInitialisedProvider(fakeSolar);

            // When: call on day 2
            var day2 = Today.AddDays(1).AddHours(8);
            scheduleProvider.TransitionRequired(day2, Today.AddHours(8), Today.AddHours(8));

            // Then: ISolarEventProvider was called once for day 1 and once for day 2
            fakeSolar.Received(1).GetEventsForDate(DateOnly.FromDateTime(Today));
            fakeSolar.Received(1).GetEventsForDate(DateOnly.FromDateTime(Today.AddDays(1)));
        }

        [Fact]
        public void IsSleep_ThrowsInvalidOperationException_WhenCalledBeforeInitialisation()
        {
            // Given: provider not yet initialised (TransitionRequired not called)
            var provider = BuildProvider(FakeSolarProvider());

            // When / Then
            Assert.Throws<InvalidOperationException>(() => provider.IsSleep(Today));
        }

        [Fact]
        public void IsSleep_ReturnsTrue_WhenBeforeSunrise()
        {
            // Given: fixed sunrise at 07:00, current time 06:59
            var provider = BuildInitialisedProvider(FakeSolarProvider());

            // When
            var result = provider.IsSleep(Today.AddHours(6).AddMinutes(59));

            // Then
            Assert.True(result);
        }

        [Fact]
        public void IsSleep_ReturnsFalse_WhenAfterSunriseAndBeforeSleepTime()
        {
            // Given: fixed sunrise at 07:00, Sleep config at 23:00, current time 12:00
            var provider = BuildInitialisedProvider(FakeSolarProvider());

            // When
            var result = provider.IsSleep(Today.AddHours(12));

            // Then
            Assert.False(result);
        }

        [Fact]
        public void IsSleep_ReturnsTrue_WhenAfterSleepTime()
        {
            // Given: Sleep config at 23:00, current time 23:01
            var provider = BuildInitialisedProvider(FakeSolarProvider());

            // When
            var result = provider.IsSleep(Today.AddHours(23).AddMinutes(1));

            // Then
            Assert.True(result);
        }
    }
}
