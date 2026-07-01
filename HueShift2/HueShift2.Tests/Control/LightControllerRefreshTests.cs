using HueShift2.Configuration.Model;
using HueShift2.Control;
using HueShift2.Interfaces;
using HueShift2.Model;
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
    public class LightControllerRefreshTests
    {
        private static HueShiftOptions DefaultOptions() => new HueShiftOptions
        {
            ColourTemperature = new ColourTemperature { Coolest = 250, Warmest = 454 },
            BasicTransitionDuration = 2,
            SyncGracePeriod = 10,
        };

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

        private static (LightController controller, ILocalHueClient hueClient) BuildController()
        {
            var hueClient = Substitute.For<ILocalHueClient>();
            var clientManager = Substitute.For<IHueClientManager>();
            var optionsMonitor = Substitute.For<IOptionsMonitor<HueShiftOptions>>();
            optionsMonitor.CurrentValue.Returns(DefaultOptions());

            var registry = new LightRegistry(NullLogger<LightRegistry>.Instance, hueClient);
            var synchroniser = new LightSynchroniser(NullLogger<LightSynchroniser>.Instance, optionsMonitor, hueClient);
            var controller = new LightController(
                NullLogger<LightController>.Instance,
                optionsMonitor,
                clientManager,
                hueClient,
                registry,
                synchroniser);

            return (controller, hueClient);
        }

        [Fact]
        public async Task NewLightDiscoveredWithCachedCommand_SyncIsDispatched()
        {
            // Given: controller has already executed a transition, caching the last command
            var (controller, hueClient) = BuildController();
            var lightA = MakeLight("1", "Living Room");
            var lightB = MakeLight("2", "Bedroom");
            var now = DateTime.UtcNow;

            // GetLightsAsync: first Refresh [A], Refresh inside Transition [A], final Refresh [A, B]
            hueClient.GetLightsAsync().Returns(
                Task.FromResult<IEnumerable<Light>>(new[] { lightA }),
                Task.FromResult<IEnumerable<Light>>(new[] { lightA }),
                Task.FromResult<IEnumerable<Light>>(new[] { lightA, lightB }));

            await controller.Refresh(now);
            var command = new LightCommand { ColorTemperature = 300, Brightness = 200 };
            await controller.Transition(new AppLightState(lightA.State), command, now, reset: false, TransitionType.Adaptive);

            // When: second Refresh discovers new light B
            await controller.Refresh(now.AddSeconds(5));

            // Then: Bridge receives a sync command for light B
            await hueClient.Received().SendCommandAsync(
                Arg.Any<LightCommand>(),
                Arg.Is<IEnumerable<string>>(ids => ids.Contains("2")));
        }

        [Fact]
        public async Task NewLightDiscoveredWithNoCachedCommand_NoSyncDispatched()
        {
            // Given: no transition has occurred (no cached command)
            var (controller, hueClient) = BuildController();
            var lightA = MakeLight("1", "Living Room");
            var now = DateTime.UtcNow;

            hueClient.GetLightsAsync().Returns(Task.FromResult<IEnumerable<Light>>(new[] { lightA }));

            // When: light A is discovered for the first time
            await controller.Refresh(now);

            // Then: no sync command sent
            await hueClient.DidNotReceive().SendCommandAsync(Arg.Any<LightCommand>(), Arg.Any<IEnumerable<string>>());
        }

        [Fact]
        public async Task LightTurnsOn_SyncIsDispatched()
        {
            // Given: a light is registered as Off on first discovery
            var (controller, hueClient) = BuildController();
            var lightOff = MakeLight("1", "Living Room", on: false);
            var lightOn = MakeLight("1", "Living Room");
            var now = DateTime.UtcNow;

            hueClient.GetLightsAsync().Returns(
                Task.FromResult<IEnumerable<Light>>(new[] { lightOff }),
                Task.FromResult<IEnumerable<Light>>(new[] { lightOn }));

            await controller.Refresh(now);

            // When: Bridge reports the light as On
            await controller.Refresh(now.AddSeconds(5));

            // Then: Bridge receives a sync command for the light
            await hueClient.Received().SendCommandAsync(
                Arg.Any<LightCommand>(),
                Arg.Is<IEnumerable<string>>(ids => ids.Contains("1")));
        }

        [Fact]
        public async Task Transition_SendsCommandToEligibleHueShiftLights()
        {
            // Given: one light on, under HueShift control, at a different CT to the target
            var (controller, hueClient) = BuildController();
            var light = MakeLight("1", "Kitchen", ct: 500);
            var now = DateTime.UtcNow;

            // GetLightsAsync called twice: once in explicit Refresh, once inside Transition's Refresh
            hueClient.GetLightsAsync().Returns(
                Task.FromResult<IEnumerable<Light>>(new[] { light }),
                Task.FromResult<IEnumerable<Light>>(new[] { light }));

            await controller.Refresh(now);

            var targetCt = 300;
            var command = new LightCommand { ColorTemperature = targetCt, Brightness = 254, TransitionTime = TimeSpan.FromSeconds(30) };
            var target = new AppLightState(light.State) { Colour = new HueShift2.Model.Colour(targetCt) };

            // When: Transition is executed
            await controller.Transition(target, command, now.AddSeconds(5), reset: false, TransitionType.Solar);

            // Then: Bridge receives the transition command for the light
            await hueClient.Received().SendCommandAsync(
                Arg.Is<LightCommand>(c => c.ColorTemperature == targetCt),
                Arg.Is<IEnumerable<string>>(ids => ids.Contains("1")));
        }
    }
}
