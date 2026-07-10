using HueShift2.Configuration.Model;
using HueShift2.Control;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using Xunit;

namespace HueShift2.Tests.Control
{
    // London (51.5°N, -0.1°E) with "Europe/London" timezone is used throughout.
    // June 21st (midsummer): astronomical sunrise ≈ 04:43 BST, sunset ≈ 21:21 BST.
    // Dec 21st (midwinter):  astronomical sunrise ≈ 08:04 GMT,  sunset ≈ 15:56 GMT.
    // Clamping limits are set to make each boundary clearly exercised.
    public class SolarEventProviderTests
    {
        private const double Latitude = 51.5;
        private const double Longitude = -0.1;
        private const string TimeZone = "Europe/London";

        private static readonly DateOnly MidSummer = new DateOnly(2024, 6, 21);
        private static readonly DateOnly MidWinter = new DateOnly(2024, 12, 21);

        private static SolarEventProvider BuildProvider(SolarTransitionTimeLimits limits)
        {
            var options = new HueShiftOptions
            {
                Geolocation = new Geolocation { Latitude = Latitude, Longitude = Longitude, TimeZone = TimeZone },
                SolarTransitionTimeLimits = limits,
            };
            var monitor = Substitute.For<IOptionsMonitor<HueShiftOptions>>();
            monitor.CurrentValue.Returns(options);
            return new SolarEventProvider(NullLogger<SolarEventProvider>.Instance, monitor);
        }

        private static SolarTransitionTimeLimits Limits(
            TimeSpan sunriseLower, TimeSpan sunriseUpper,
            TimeSpan sunsetLower, TimeSpan sunsetUpper) =>
            new SolarTransitionTimeLimits
            {
                SunriseLower = sunriseLower,
                SunriseUpper = sunriseUpper,
                SunsetLower = sunsetLower,
                SunsetUpper = sunsetUpper,
            };

        [Fact]
        public void Sunrise_IsClamped_WhenBeforeLowerBound()
        {
            // Given: London midsummer — astronomical sunrise ≈ 04:43 BST, below lower bound of 06:00
            var provider = BuildProvider(Limits(
                sunriseLower: TimeSpan.FromHours(6),
                sunriseUpper: TimeSpan.FromHours(8),
                sunsetLower: TimeSpan.FromHours(18),
                sunsetUpper: TimeSpan.FromHours(22)));

            // When
            var events = provider.GetEventsForDate(MidSummer);

            // Then: Sunrise is clamped to SunriseLower
            Assert.Equal(TimeSpan.FromHours(6), events.Sunrise.TimeOfDay);
        }

        [Fact]
        public void Sunrise_IsClamped_WhenAfterUpperBound()
        {
            // Given: London midwinter — astronomical sunrise ≈ 08:04 GMT, above upper bound of 07:00
            var provider = BuildProvider(Limits(
                sunriseLower: TimeSpan.FromHours(6),
                sunriseUpper: TimeSpan.FromHours(7),
                sunsetLower: TimeSpan.FromHours(14),
                sunsetUpper: TimeSpan.FromHours(20)));

            // When
            var events = provider.GetEventsForDate(MidWinter);

            // Then: Sunrise is clamped to SunriseUpper
            Assert.Equal(TimeSpan.FromHours(7), events.Sunrise.TimeOfDay);
        }

        [Fact]
        public void Sunset_IsClamped_WhenBeforeLowerBound()
        {
            // Given: London midwinter — astronomical sunset ≈ 15:56 GMT, before lower bound of 18:00
            var provider = BuildProvider(Limits(
                sunriseLower: TimeSpan.FromHours(6),
                sunriseUpper: TimeSpan.FromHours(9),
                sunsetLower: TimeSpan.FromHours(18),
                sunsetUpper: TimeSpan.FromHours(20)));

            // When
            var events = provider.GetEventsForDate(MidWinter);

            // Then: Sunset is clamped to SunsetLower
            Assert.Equal(TimeSpan.FromHours(18), events.Sunset.TimeOfDay);
        }

        [Fact]
        public void Sunset_IsClamped_WhenAfterUpperBound()
        {
            // Given: London midsummer — astronomical sunset ≈ 21:21 BST, above upper bound of 20:00
            var provider = BuildProvider(Limits(
                sunriseLower: TimeSpan.FromHours(4),
                sunriseUpper: TimeSpan.FromHours(8),
                sunsetLower: TimeSpan.FromHours(18),
                sunsetUpper: TimeSpan.FromHours(20)));

            // When
            var events = provider.GetEventsForDate(MidSummer);

            // Then: Sunset is clamped to SunsetUpper
            Assert.Equal(TimeSpan.FromHours(20), events.Sunset.TimeOfDay);
        }

        [Fact]
        public void ReturnedEvents_AlwaysSatisfy_SunriseBeforeSolarNoonBeforeSunset()
        {
            // Given: limits that produce clamped sunrise and sunset
            var provider = BuildProvider(Limits(
                sunriseLower: TimeSpan.FromHours(6),
                sunriseUpper: TimeSpan.FromHours(8),
                sunsetLower: TimeSpan.FromHours(18),
                sunsetUpper: TimeSpan.FromHours(20)));

            var summerEvents = provider.GetEventsForDate(MidSummer);
            var winterEvents = provider.GetEventsForDate(MidWinter);

            Assert.True(summerEvents.Sunrise < summerEvents.SolarNoon);
            Assert.True(summerEvents.SolarNoon < summerEvents.Sunset);
            Assert.True(winterEvents.Sunrise < winterEvents.SolarNoon);
            Assert.True(winterEvents.SolarNoon < winterEvents.Sunset);
        }

        [Fact]
        public void ReturnedEvents_Sunrise_And_Sunset_AreOnSameDate()
        {
            // Given: standard limits, London midsummer
            var provider = BuildProvider(Limits(
                sunriseLower: TimeSpan.FromHours(6),
                sunriseUpper: TimeSpan.FromHours(8),
                sunsetLower: TimeSpan.FromHours(18),
                sunsetUpper: TimeSpan.FromHours(20)));

            // When
            var events = provider.GetEventsForDate(MidSummer);

            // Then
            Assert.Equal(events.Sunrise.Date, events.Sunset.Date);
        }
    }
}
