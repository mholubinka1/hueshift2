using HueShift2.Control;
using HueShift2.Model;
using Q42.HueApi;
using System;
using Xunit;

namespace HueShift2.Tests.Control
{
    public class LightControlPairUnreachableTests
    {
        private const int Coolest = 250;
        private const int Warmest = 454;
        private static readonly DateTime Now = DateTime.UtcNow;

        private static LightControlPair LightOnAt(int ct) => new LightControlPair(new Light
        {
            Id = "1",
            Name = "Test Light",
            State = BridgeCt(ct),
        });

        private static State BridgeCt(int ct, bool on = true) => new State
        {
            On = on,
            Brightness = 254,
            ColorMode = "ct",
            ColorTemperature = ct,
            IsReachable = true,
        };

        private static State BridgeUnreachable() => new State
        {
            IsReachable = false,
        };

        [Fact]
        public void ExpectedBrightnessPreservedWhenLightGoesUnreachable()
        {
            // Given: a HueShiftControlled light with brightness 254
            var pair = LightOnAt(309);
            pair.Refresh(BridgeCt(309), Now, Coolest, Warmest);
            Assert.Equal((byte)254, pair.ExpectedLight.Brightness);

            // When: light goes unreachable (bridge returns null brightness)
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(5), Coolest, Warmest);

            // Then: expected brightness is preserved — not overwritten with null
            Assert.Equal((byte)254, pair.ExpectedLight.Brightness);
        }

        [Fact]
        public void HueShiftControlledLightReconnectingSetsSyncRequired()
        {
            // Given: a HueShiftControlled light that was on
            var pair = LightOnAt(309);

            // And: light goes unreachable
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(5), Coolest, Warmest);

            // When: light reconnects
            pair.Refresh(BridgeCt(309), Now.AddSeconds(10), Coolest, Warmest);

            // Then: SyncRequired is set — reconnect is treated like power-on
            Assert.True(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }

        [Fact]
        public void DriftCheckSuppressedWhileUnreachable()
        {
            // Given: a HueShiftControlled light on target CT
            var pair = LightOnAt(309);
            pair.Refresh(BridgeCt(309), Now, Coolest, Warmest);

            // When: light goes unreachable (bridge reports wrong CT alongside unreachable)
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(5), Coolest, Warmest);

            // Then: no drift or SyncRequired — checks suppressed while unreachable
            Assert.False(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }

        [Fact]
        public void ManualOverrideNotDetectedWhileUnreachable()
        {
            // Given: a HueShiftControlled light on a valid in-range CT
            var pair = LightOnAt(309);
            pair.Refresh(BridgeCt(309), Now, Coolest, Warmest);

            // When: light goes unreachable (IsReachable=false, no CT data)
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(5), Coolest, Warmest);

            // Then: Manual Override not triggered — checks suppressed while unreachable
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }

        [Fact]
        public void ManualOverridePreservedWhenLightReconnects()
        {
            // Given: a light in Manual Override (CT out of range)
            var pair = LightOnAt(309);
            pair.Refresh(BridgeCt(200), Now, Coolest, Warmest);
            Assert.Equal(LightControlState.Manual, pair.AppControlState);

            // And: light goes unreachable — this is not a power cycle
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(5), Coolest, Warmest);

            // When: light reconnects
            pair.Refresh(BridgeCt(309), Now.AddSeconds(10), Coolest, Warmest);

            // Then: Manual Override preserved — reconnect is not a power cycle
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
            Assert.False(pair.SyncRequired);
        }

        [Fact]
        public void ManualOverrideClearedWhenLightPowerCycled()
        {
            // Given: a light in Manual Override
            var pair = LightOnAt(309);
            pair.Refresh(BridgeCt(200), Now, Coolest, Warmest); // CT=200 < Coolest=250
            Assert.Equal(LightControlState.Manual, pair.AppControlState);

            // And: light turns off while bridge remains reachable — genuine power cycle
            pair.Refresh(BridgeCt(200, on: false), Now.AddSeconds(5), Coolest, Warmest);

            // When: light turns back on
            pair.Refresh(BridgeCt(309), Now.AddSeconds(10), Coolest, Warmest);

            // Then: Manual Override cleared and sync queued
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
            Assert.True(pair.SyncRequired);
        }

        [Fact]
        public void HueShiftControlledLightReconnectsCleanlyAfterTransitionExpiredDuringOutage()
        {
            // Given: a light mid-transition (1s commanded + 10s settling = 11s window)
            var pair = LightOnAt(309);
            pair.ExecuteCommand(
                new LightCommand { ColorTemperature = 309, TransitionTime = TimeSpan.FromSeconds(1) },
                Now,
                TransitionType.Sync);

            // And: light goes unreachable at T+5s — mid settling window
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(5), Coolest, Warmest);

            // And: transition timer expires during the outage (T+15s > 11s window)
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(15), Coolest, Warmest);
            Assert.False(pair.IsTransitioning()); // timer cleared during outage

            // When: light reconnects at T+20s
            pair.Refresh(BridgeCt(309), Now.AddSeconds(20), Coolest, Warmest);

            // Then: light is not transitioning and sync is queued — reconnect treated as power-on
            Assert.False(pair.IsTransitioning());
            Assert.True(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }

        [Fact]
        public void TransitioningFlagClearedWhileUnreachableAfterTimerExpires()
        {
            // Given: a HueShiftControlled light with a 1s Sync transition (11s internal window)
            var pair = LightOnAt(309);
            pair.ExecuteCommand(
                new LightCommand { ColorTemperature = 309, TransitionTime = TimeSpan.FromSeconds(1) },
                Now,
                TransitionType.Sync);
            Assert.True(pair.IsTransitioning());

            // And: light goes unreachable at T+5s — still within settling window
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(5), Coolest, Warmest);
            Assert.True(pair.IsTransitioning());

            // When: polled again while still unreachable at T+15s — past the 11s window
            pair.Refresh(BridgeUnreachable(), Now.AddSeconds(15), Coolest, Warmest);

            // Then: IsTransitioning is false — timer expired during the outage
            Assert.False(pair.IsTransitioning());
        }
    }
}
