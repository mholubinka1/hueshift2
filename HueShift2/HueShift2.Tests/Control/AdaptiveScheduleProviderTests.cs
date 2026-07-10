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
            var scheduleProvider = BuildProvider(FakeSolarProvider());
            scheduleProvider.TransitionRequired(Today.AddHours(8), null, null); // initialise

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
            var scheduleProvider = BuildProvider(FakeSolarProvider());
            scheduleProvider.TransitionRequired(Today.AddHours(8), null, null); // initialise

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
            var scheduleProvider = BuildProvider(FakeSolarProvider());
            scheduleProvider.TransitionRequired(Today.AddHours(8), null, null); // initialise

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
            var scheduleProvider = BuildProvider(FakeSolarProvider());
            scheduleProvider.TransitionRequired(Today.AddHours(8), null, null); // initialise

            var currentTime = Today.AddHours(10);
            var lastRunTime = currentTime.AddSeconds(-30);
            var lastTransitionTime = currentTime.AddSeconds(-30);

            // When
            var result = scheduleProvider.TransitionRequired(currentTime, lastRunTime, lastTransitionTime);

            // Then
            Assert.Equal(TransitionType.Null, result);
        }
    }
}
