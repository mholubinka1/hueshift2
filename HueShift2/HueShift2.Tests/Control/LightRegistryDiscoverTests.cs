using HueShift2.Configuration.Model;
using HueShift2.Control;
using HueShift2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HueShift2.Tests.Control
{
    public class LightRegistryDiscoverTests
    {
        private static readonly DateTime Now = DateTime.UtcNow;
        private static readonly ColourTemperature DefaultCt = new ColourTemperature { Coolest = 250, Warmest = 454 };

        private static Light MakeLight(string id, string name, int ct = 339, bool on = true) => new Light
        {
            Id = id,
            Name = name,
            State = new State
            {
                On = on,
                Brightness = 254,
                ColorMode = "ct",
                ColorTemperature = ct,
                IsReachable = true,
            }
        };

        private static (LightRegistry registry, ILocalHueClient hueClient, IOptionsMonitor<HueShiftOptions> optionsMonitor)
            BuildRegistry(HueShiftOptions? options = null)
        {
            var hueClient = Substitute.For<ILocalHueClient>();
            var optionsMonitor = Substitute.For<IOptionsMonitor<HueShiftOptions>>();
            optionsMonitor.CurrentValue.Returns(options ?? DefaultOptions());
            var registry = new LightRegistry(NullLogger<LightRegistry>.Instance, hueClient, optionsMonitor);
            return (registry, hueClient, optionsMonitor);
        }

        private static HueShiftOptions DefaultOptions(IList<string>? lightsToExclude = null) => new HueShiftOptions
        {
            ColourTemperature = DefaultCt,
            BasicTransitionDuration = 2,
            LightsToExclude = lightsToExclude ?? new List<string>(),
        };

        [Fact]
        public async Task LightWithIdInExclusionList_IsExcludedAfterDiscover()
        {
            // Given: a registry with light "1" in the exclusion list
            var options = DefaultOptions(lightsToExclude: new List<string> { "1" });
            var (registry, hueClient, _) = BuildRegistry(options);
            var light = MakeLight("1", "Living Room");
            hueClient.GetLightsAsync().Returns(Task.FromResult<IEnumerable<Light>>(new[] { light }));

            // When: Discover is called
            await registry.Discover(cachedCommand: null, Now, DefaultCt);

            // Then: the light is in Excluded state
            var lights = registry.GetAll();
            Assert.Equal(LightControlState.Excluded, lights["1"].AppControlState);
        }

        [Fact]
        public async Task LightWithNameInExclusionList_IsExcludedAfterDiscover()
        {
            // Given: a registry with "Living Room" in the exclusion list (by Name)
            var options = DefaultOptions(lightsToExclude: new List<string> { "Living Room" });
            var (registry, hueClient, _) = BuildRegistry(options);
            var light = MakeLight("1", "Living Room");
            hueClient.GetLightsAsync().Returns(Task.FromResult<IEnumerable<Light>>(new[] { light }));

            // When: Discover is called
            await registry.Discover(cachedCommand: null, Now, DefaultCt);

            // Then: the light is in Excluded state
            var lights = registry.GetAll();
            Assert.Equal(LightControlState.Excluded, lights["1"].AppControlState);
        }

        [Fact]
        public async Task ExcludedLightOnFirstDiscovery_DoesNotReceiveSyncWithCachedCommand()
        {
            // Given: a registry with light "1" excluded, and a prior cached command
            var options = DefaultOptions(lightsToExclude: new List<string> { "1" });
            var (registry, hueClient, _) = BuildRegistry(options);
            var light = MakeLight("1", "Living Room");

            // Two GetLightsAsync calls: first for non-excluded light to establish cached command,
            // second for the excluded light discovery
            var otherLight = MakeLight("2", "Kitchen");
            hueClient.GetLightsAsync().Returns(
                Task.FromResult<IEnumerable<Light>>(new[] { otherLight }),
                Task.FromResult<IEnumerable<Light>>(new[] { light }));

            // First discover populates a cached command scenario via the controller —
            // here we test the registry directly: an excluded new light gets no MarkForSync even with a cachedCommand
            var cachedCommand = new Q42.HueApi.LightCommand { ColorTemperature = 300, Brightness = 200 };
            await registry.Discover(cachedCommand, Now, DefaultCt);
            await registry.Discover(cachedCommand, Now.AddSeconds(2), DefaultCt);

            // Then: light "1" is Excluded and SyncRequired is false
            var lights = registry.GetAll();
            Assert.Equal(LightControlState.Excluded, lights["1"].AppControlState);
            Assert.False(lights["1"].SyncRequired);
        }

        [Fact]
        public async Task LightAddedToExclusionList_IsExcludedOnNextDiscover()
        {
            // Given: a light under HueShift control with an empty exclusion list
            var options = DefaultOptions();
            var (registry, hueClient, optionsMonitor) = BuildRegistry(options);
            var light = MakeLight("1", "Bedroom");
            hueClient.GetLightsAsync().Returns(Task.FromResult<IEnumerable<Light>>(new[] { light }));

            await registry.Discover(cachedCommand: null, Now, DefaultCt);
            Assert.Equal(LightControlState.HueShiftControlled, registry.GetAll()["1"].AppControlState);

            // When: light "1" is added to the exclusion list and Discover is called again
            optionsMonitor.CurrentValue.Returns(DefaultOptions(lightsToExclude: new List<string> { "1" }));
            await registry.Discover(cachedCommand: null, Now.AddSeconds(2), DefaultCt);

            // Then: light transitions to Excluded
            Assert.Equal(LightControlState.Excluded, registry.GetAll()["1"].AppControlState);
        }

        [Fact]
        public async Task LightRemovedFromExclusionList_ReturnsToHueShiftControlledWithSync()
        {
            // Given: a light that starts excluded
            var options = DefaultOptions(lightsToExclude: new List<string> { "1" });
            var (registry, hueClient, optionsMonitor) = BuildRegistry(options);
            var light = MakeLight("1", "Bedroom");
            hueClient.GetLightsAsync().Returns(Task.FromResult<IEnumerable<Light>>(new[] { light }));

            await registry.Discover(cachedCommand: null, Now, DefaultCt);
            Assert.Equal(LightControlState.Excluded, registry.GetAll()["1"].AppControlState);

            // When: light "1" is removed from the exclusion list and Discover is called again
            optionsMonitor.CurrentValue.Returns(DefaultOptions());
            await registry.Discover(cachedCommand: null, Now.AddSeconds(2), DefaultCt);

            // Then: light returns to HueShiftControlled with SyncRequired set
            var pair = registry.GetAll()["1"];
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
            Assert.True(pair.SyncRequired);
        }

        [Fact]
        public async Task UnmatchedExclusionEntry_LogsWarningOnce()
        {
            // Given: a registry with an exclusion entry that matches no light
            var logger = Substitute.For<ILogger<LightRegistry>>();
            var hueClient = Substitute.For<ILocalHueClient>();
            var optionsMonitor = Substitute.For<IOptionsMonitor<HueShiftOptions>>();
            optionsMonitor.CurrentValue.Returns(DefaultOptions(lightsToExclude: new List<string> { "ghost-id" }));
            var registry = new LightRegistry(logger, hueClient, optionsMonitor);

            var light = MakeLight("1", "Kitchen");
            hueClient.GetLightsAsync().Returns(Task.FromResult<IEnumerable<Light>>(new[] { light }));

            // When: Discover is called twice
            await registry.Discover(cachedCommand: null, Now, DefaultCt);
            await registry.Discover(cachedCommand: null, Now.AddSeconds(2), DefaultCt);

            // Then: the warning is logged exactly once
            logger.Received(1).Log(
                LogLevel.Warning,
                Arg.Any<EventId>(),
                Arg.Is<object>(o => o.ToString()!.Contains("ghost-id")),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }
    }
}
