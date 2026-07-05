using HueShift2.Control;
using HueShift2.Model;
using Q42.HueApi;
using System;
using Xunit;

namespace HueShift2.Tests.Control
{
    public class LightControlPairTransitionSettlingTests
    {
        private const int Coolest = 250;
        private const int Warmest = 454;
        private static readonly DateTime Now = DateTime.UtcNow;

        private static LightControlPair LightOnAt(int ct)
        {
            return new LightControlPair(new Light
            {
                Id = "1",
                Name = "Test Light",
                State = BridgeCt(ct),
            });
        }

        private static State BridgeCt(int ct, bool on = true) => new State
        {
            On = on,
            Brightness = 254,
            ColorMode = "ct",
            ColorTemperature = ct,
            IsReachable = true,
        };

        [Fact]
        public void DriftCheckSuppressedAfterTransitionCompletesWithinSettlingPeriod()
        {
            // Given: a HueShift light that received a 1s Sync command
            var pair = LightOnAt(309);
            pair.ExecuteCommand(
                new LightCommand { ColorTemperature = 309, TransitionTime = TimeSpan.FromSeconds(1) },
                Now,
                TransitionType.Sync);
            // And: the commanded transition has elapsed — first poll at T+2s, bridge on target CT
            pair.Refresh(BridgeCt(309), Now.AddSeconds(2), Coolest, Warmest);

            // When: bridge reports drift at T+5s (within the 10s Transition Settling Period)
            pair.Refresh(BridgeCt(320), Now.AddSeconds(5), Coolest, Warmest);

            // Then: drift check suppressed — Transition Settling Period still active
            Assert.False(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }
        [Fact]
        public void DriftCheckFiresAfterSettlingPeriodExpires()
        {
            // Given: a HueShift light that received a 1s Sync command
            var pair = LightOnAt(309);
            pair.ExecuteCommand(
                new LightCommand { ColorTemperature = 309, TransitionTime = TimeSpan.FromSeconds(1) },
                Now,
                TransitionType.Sync);
            // And: settling period has elapsed — poll at T+12s (1s commanded + 10s settling + 1s)
            pair.Refresh(BridgeCt(309), Now.AddSeconds(12), Coolest, Warmest);

            // When: bridge reports drift on the next poll
            pair.Refresh(BridgeCt(320), Now.AddSeconds(15), Coolest, Warmest);

            // Then: drift check fires — Transition Settling Period has expired
            Assert.True(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }
        [Fact]
        public void ManualOverrideCheckSuppressedWithinSettlingPeriod()
        {
            // Given: a HueShift light that received a 1s Sync command
            var pair = LightOnAt(309);
            pair.ExecuteCommand(
                new LightCommand { ColorTemperature = 309, TransitionTime = TimeSpan.FromSeconds(1) },
                Now,
                TransitionType.Sync);
            // And: the commanded transition has elapsed — poll at T+2s, bridge on target CT
            pair.Refresh(BridgeCt(309), Now.AddSeconds(2), Coolest, Warmest);

            // When: bridge reports an out-of-range CT at T+5s (within settling period)
            pair.Refresh(BridgeCt(455), Now.AddSeconds(5), Coolest, Warmest);

            // Then: Manual Override not detected — settling period suppresses checks
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }
        [Fact]
        public void SettlingPeriodAppliesAfterAdaptiveTransition()
        {
            // Given: a HueShift light that received a 2s Adaptive transition
            var pair = LightOnAt(400);
            pair.ExecuteCommand(
                new LightCommand { ColorTemperature = 309, TransitionTime = TimeSpan.FromSeconds(2) },
                Now,
                TransitionType.Adaptive);
            // And: the commanded transition has elapsed — poll at T+3s, bridge on target CT
            pair.Refresh(BridgeCt(309), Now.AddSeconds(3), Coolest, Warmest);

            // When: bridge reports drift at T+8s (within 10s settling period after 2s commanded)
            pair.Refresh(BridgeCt(320), Now.AddSeconds(8), Coolest, Warmest);

            // Then: drift check suppressed — Transition Settling Period still active
            Assert.False(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }
    }
}
