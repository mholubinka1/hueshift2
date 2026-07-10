using HueShift2.Configuration.Model;
using HueShift2.Control;
using HueShift2.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HueShift2.Tests.Control
{
    public class AdaptiveLightColourCalculatorTests
    {
        private const int Coolest = 250;
        private const int Warmest = 454;

        private static readonly DateTime Sunrise = new DateTime(2025, 6, 21, 6, 0, 0);
        private static readonly DateTime SolarNoon = new DateTime(2025, 6, 21, 12, 0, 0);
        private static readonly DateTime Sunset = new DateTime(2025, 6, 21, 18, 0, 0);
        private static readonly AdaptiveSolarEvents StandardDay = new AdaptiveSolarEvents(Sunrise, SolarNoon, Sunset);

        private static AdaptiveLightColourCalculator BuildCalculator(int coolest = Coolest, int warmest = Warmest)
        {
            var options = new HueShiftOptions
            {
                ColourTemperature = new ColourTemperature { Coolest = coolest, Warmest = warmest }
            };
            var optionsMonitor = Substitute.For<IOptionsMonitor<HueShiftOptions>>();
            optionsMonitor.CurrentValue.Returns(options);
            return new AdaptiveLightColourCalculator(NullLogger<AdaptiveLightColourCalculator>.Instance, optionsMonitor);
        }

        private static AppLightState Calculate(AdaptiveLightColourCalculator calculator, DateTime currentTime)
        {
            // CT is resolved from IOptionsMonitor inside the calculator; the ColourTemperature argument to AdaptiveCalculationParameters is unused.
            var parameters = new AdaptiveCalculationParameters(StandardDay, new ColourTemperature());
            return calculator.SetBrightnessAndColour(parameters, currentTime, isSleep: false);
        }

        // Sun position boundary cases

        [Fact]
        public void SunPosition_BeforeSunrise_ProducesWarmestColourTemperature()
        {
            // Given: calculator with Coolest=250, Warmest=454 and a standard day (06:00–18:00)
            var calculator = BuildCalculator();

            // When: current time is 04:00 (before sunrise)
            var result = Calculate(calculator, new DateTime(2025, 6, 21, 4, 0, 0));

            // Then: sun is below the horizon — colour temperature equals Warmest
            Assert.Equal(Warmest, result.Colour.ColourTemperature);
        }

        [Fact]
        public void SunPosition_AtSunrise_ProducesWarmestColourTemperature()
        {
            // Given: calculator and standard day
            var calculator = BuildCalculator();

            // When: current time is exactly at sunrise (06:00)
            var result = Calculate(calculator, Sunrise);

            // Then: sun position is 0.0 (horizon) — colour temperature equals Warmest
            Assert.Equal(Warmest, result.Colour.ColourTemperature);
        }

        [Fact]
        public void SunPosition_BetweenSunriseAndNoon_ProducesIntermediateColourTemperature()
        {
            // Given: calculator and standard day
            var calculator = BuildCalculator();

            // When: current time is 09:00 (midway between sunrise and solar noon)
            var result = Calculate(calculator, new DateTime(2025, 6, 21, 9, 0, 0));

            // Then: sun position is in (0.0, 1.0) — CT is strictly between Coolest and Warmest
            Assert.InRange(result.Colour.ColourTemperature!.Value, Coolest + 1, Warmest - 1);
        }

        [Fact]
        public void SunPosition_AtSolarNoon_ProducesCoolestColourTemperature()
        {
            // Given: calculator and standard day
            var calculator = BuildCalculator();

            // When: current time is exactly at solar noon (12:00)
            var result = Calculate(calculator, SolarNoon);

            // Then: sun is at zenith (position 1.0) — colour temperature equals Coolest
            Assert.Equal(Coolest, result.Colour.ColourTemperature);
        }

        [Fact]
        public void SunPosition_BetweenNoonAndSunset_ProducesIntermediateColourTemperature()
        {
            // Given: calculator and standard day
            var calculator = BuildCalculator();

            // When: current time is 15:00 (midway between solar noon and sunset)
            var result = Calculate(calculator, new DateTime(2025, 6, 21, 15, 0, 0));

            // Then: sun position is in (0.0, 1.0) — CT is strictly between Coolest and Warmest
            Assert.InRange(result.Colour.ColourTemperature!.Value, Coolest + 1, Warmest - 1);
        }

        [Fact]
        public void SunPosition_AtSunset_ProducesWarmestColourTemperature()
        {
            // Given: calculator and standard day
            var calculator = BuildCalculator();

            // When: current time is exactly at sunset (18:00)
            var result = Calculate(calculator, Sunset);

            // Then: sun position is 0.0 (horizon) — colour temperature equals Warmest
            Assert.Equal(Warmest, result.Colour.ColourTemperature);
        }

        [Fact]
        public void SunPosition_AfterSunset_ProducesWarmestColourTemperature()
        {
            // Given: calculator and standard day
            var calculator = BuildCalculator();

            // When: current time is 22:00 (after sunset)
            var result = Calculate(calculator, new DateTime(2025, 6, 21, 22, 0, 0));

            // Then: sun is below the horizon — colour temperature equals Warmest
            Assert.Equal(Warmest, result.Colour.ColourTemperature);
        }

        // Colour temperature mapping cases

        [Fact]
        public void ColourTemperature_WhenSunAtZenith_IsConfiguredCoolest()
        {
            // Given: non-default CT values to prove config is read (Coolest=300, Warmest=400)
            var calculator = BuildCalculator(coolest: 300, warmest: 400);

            // When: current time is exactly solar noon (sun position = 1.0)
            var result = Calculate(calculator, SolarNoon);

            // Then: CT equals the configured Coolest, not the default 250
            Assert.Equal(300, result.Colour.ColourTemperature);
        }

        [Fact]
        public void ColourTemperature_WhenSunBelowHorizon_IsConfiguredWarmest()
        {
            // Given: non-default CT values (Coolest=300, Warmest=400)
            var calculator = BuildCalculator(coolest: 300, warmest: 400);

            // When: current time is before sunrise (sun position = -1.0)
            var result = Calculate(calculator, new DateTime(2025, 6, 21, 4, 0, 0));

            // Then: CT equals the configured Warmest, not the default 454
            Assert.Equal(400, result.Colour.ColourTemperature);
        }

        [Fact]
        public void ColourTemperature_WhenSunAtHorizon_IsConfiguredWarmest()
        {
            // Given: non-default CT values (Coolest=300, Warmest=400)
            var calculator = BuildCalculator(coolest: 300, warmest: 400);

            // When: current time is exactly at sunrise (sun position = 0.0)
            var result = Calculate(calculator, Sunrise);

            // Then: CT equals the configured Warmest — horizon maps to warm, not cool
            Assert.Equal(400, result.Colour.ColourTemperature);
        }
    }
}
