using HueShift2.Control;
using HueShift2.Model;
using Q42.HueApi;
using System;
using Xunit;

namespace HueShift2.Tests.Control
{
    public class LightControlPairManualOverrideTests
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

        private static State BridgeXy(double x, double y, bool on = true) => new State
        {
            On = on,
            Brightness = 254,
            ColorMode = "xy",
            ColorCoordinates = new[] { x, y },
            IsReachable = true,
        };

        [Fact]
        public void LightRevertsToInRangeCt_StaysHueShift()
        {
            // Given: a HueShift light synced to CT 309
            var pair = LightOnAt(309);

            // When: bridge reports CT 454 (TRADFRI power-on default — within [250, 454])
            pair.Refresh(BridgeCt(454), Now, Coolest, Warmest);

            // Then: light stays HueShift-controlled and a re-sync is queued
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
            Assert.True(pair.SyncRequired);
        }

        [Fact]
        public void LightAboveWarmestBoundary_IsManualOverride()
        {
            // Given: a HueShift light on
            var pair = LightOnAt(309);

            // When: bridge reports CT 455 (one above Warmest 454 — outside operating range)
            pair.Refresh(BridgeCt(455), Now, Coolest, Warmest);

            // Then: Manual Override detected
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
        }

        [Fact]
        public void LightBelowCoolestBoundary_IsManualOverride()
        {
            // Given: a HueShift light on
            var pair = LightOnAt(309);

            // When: bridge reports CT 249 (one below Coolest 250 — outside operating range)
            pair.Refresh(BridgeCt(249), Now, Coolest, Warmest);

            // Then: Manual Override detected
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
        }

        [Fact]
        public void LightAtExactWarmestBoundary_StaysHueShift()
        {
            var pair = LightOnAt(309);
            pair.Refresh(BridgeCt(Warmest), Now, Coolest, Warmest);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
        }

        [Fact]
        public void LightAtExactCoolestBoundary_StaysHueShift()
        {
            var pair = LightOnAt(309);
            pair.Refresh(BridgeCt(Coolest), Now, Coolest, Warmest);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
        }

        [Fact]
        public void InRangeDriftAboveTolerance_SetsSyncRequired()
        {
            // Given: a HueShift light with expected CT 309
            var pair = LightOnAt(309);

            // When: bridge reports CT 320 (drift of 11 mired — above 10 mired tolerance)
            pair.Refresh(BridgeCt(320), Now, Coolest, Warmest);

            // Then: SyncRequired is set, control stays HueShift
            Assert.True(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
        }

        [Fact]
        public void InRangeDriftAtTolerance_DoesNotSetSyncRequired()
        {
            // Given: a HueShift light with expected CT 309
            var pair = LightOnAt(309);

            // When: bridge reports CT 319 (drift of exactly 10 mired — within tolerance)
            pair.Refresh(BridgeCt(319), Now, Coolest, Warmest);

            // Then: no sync triggered
            Assert.False(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
        }

        [Fact]
        public void XyModeWithInRangeConvertedCt_StaysHueShiftAndSetsSyncRequired()
        {
            // Given: a HueShift light with expected CT 309
            var pair = LightOnAt(309);

            // When: bridge reports XY that converts to ~370 mired (within [250,454], >10 from 309)
            // XY [0.4448, 0.4066] ≈ 2700K ≈ 370 mired
            pair.Refresh(BridgeXy(0.4448, 0.4066), Now, Coolest, Warmest);

            // Then: not Manual, but drift sync triggered
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
            Assert.True(pair.SyncRequired);
        }

        [Fact]
        public void XyModeWhereConversionFails_IsManualOverride()
        {
            // Given: a HueShift light on
            var pair = LightOnAt(309);

            // When: bridge reports XY that produces a degenerate (nonsensical) CT conversion
            // XY [0.3320, 0.1858] causes division by zero in the McCamy formula denominator
            pair.Refresh(BridgeXy(0.3320, 0.1858), Now, Coolest, Warmest);

            // Then: Manual Override
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
        }

        [Fact]
        public void XyModeWithOutOfRangeConvertedCt_IsManualOverride()
        {
            // Given: a HueShift light on
            var pair = LightOnAt(309);

            // When: bridge reports XY corresponding to a very cool colour (CT well below 250)
            // XY [0.1727, 0.0456] ≈ 12000K ≈ 83 mired — far outside [250, 454]
            pair.Refresh(BridgeXy(0.1727, 0.0456), Now, Coolest, Warmest);

            // Then: Manual Override
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
        }

        [Fact]
        public void DriftCheckDoesNotFireDuringAdaptiveTransition()
        {
            // Given: a HueShift light mid-Adaptive Transition (PowerState = Transitioning)
            var pair = LightOnAt(309);
            var transitionDuration = TimeSpan.FromSeconds(30);
            pair.ExecuteCommand(
                new LightCommand { ColorTemperature = 300, TransitionTime = transitionDuration },
                Now,
                TransitionType.Adaptive);

            // When: bridge reports an out-of-range CT during the transition
            pair.Refresh(BridgeCt(455), Now.AddSeconds(5), Coolest, Warmest);

            // Then: no Manual Override — checks are suppressed while Transitioning
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
            Assert.False(pair.SyncRequired);
        }

        [Fact]
        public void LightTurnsOn_SetsSyncRequired()
        {
            // Given: a HueShift light that was Off
            var pair = new LightControlPair(new Light
            {
                Id = "1",
                Name = "Test Light",
                State = BridgeCt(309, on: false),
            });

            // When: bridge reports the light On
            pair.Refresh(BridgeCt(309), Now, Coolest, Warmest);

            // Then: SyncRequired is set
            Assert.True(pair.SyncRequired);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
        }

        [Fact]
        public void ResetClearsManualAndSetsSyncRequired()
        {
            // Given: a light that has been manually overridden
            var pair = LightOnAt(309);
            pair.Refresh(BridgeCt(455), Now, Coolest, Warmest);
            Assert.Equal(LightControlState.Manual, pair.AppControlState);

            // When: Reset is called (Solar or FirstRun transition)
            pair.Reset();

            // Then: control returns to HueShift and sync is flagged
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
            Assert.True(pair.SyncRequired);
        }
    }
}
