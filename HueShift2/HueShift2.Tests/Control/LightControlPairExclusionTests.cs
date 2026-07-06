using HueShift2.Control;
using HueShift2.Model;
using Q42.HueApi;
using Xunit;

namespace HueShift2.Tests.Control
{
    public class LightControlPairExclusionTests
    {
        private static LightControlPair HueShiftControlledLight() =>
            new LightControlPair(new Light
            {
                Id = "1",
                Name = "Test Light",
                State = new State
                {
                    On = true,
                    Brightness = 254,
                    ColorMode = "ct",
                    ColorTemperature = 309,
                    IsReachable = true,
                }
            });

        [Fact]
        public void Exclude_SetsStateToExcluded()
        {
            // Given: a HueShift-controlled light pair
            var pair = HueShiftControlledLight();

            // When: Exclude() is called
            pair.Exclude();

            // Then: state is Excluded
            Assert.Equal(LightControlState.Excluded, pair.AppControlState);
        }

        [Fact]
        public void Exclude_ClearsSyncRequired()
        {
            // Given: a HueShift-controlled light with a pending sync
            var pair = HueShiftControlledLight();
            pair.MarkForSync();
            Assert.True(pair.SyncRequired);

            // When: Exclude() is called
            pair.Exclude();

            // Then: SyncRequired is cleared
            Assert.False(pair.SyncRequired);
        }

        [Fact]
        public void Unexclude_SetsStateToHueShiftControlled()
        {
            // Given: an excluded light pair
            var pair = HueShiftControlledLight();
            pair.Exclude();

            // When: Unexclude() is called
            pair.Unexclude();

            // Then: state returns to HueShiftControlled
            Assert.Equal(LightControlState.HueShiftControlled, pair.AppControlState);
        }

        [Fact]
        public void Unexclude_SetsSyncRequired()
        {
            // Given: an excluded light pair
            var pair = HueShiftControlledLight();
            pair.Exclude();

            // When: Unexclude() is called
            pair.Unexclude();

            // Then: SyncRequired is set
            Assert.True(pair.SyncRequired);
        }

        [Fact]
        public void Exclude_OnAlreadyExcludedPair_IsIdempotent()
        {
            // Given: an already-excluded light pair
            var pair = HueShiftControlledLight();
            pair.Exclude();
            Assert.Equal(LightControlState.Excluded, pair.AppControlState);
            Assert.False(pair.SyncRequired);

            // When: Exclude() is called again
            pair.Exclude();

            // Then: state remains Excluded and SyncRequired remains false
            Assert.Equal(LightControlState.Excluded, pair.AppControlState);
            Assert.False(pair.SyncRequired);
        }

        [Fact]
        public void Reset_OnExcludedPair_IsNoOp()
        {
            // Given: an excluded light pair
            var pair = HueShiftControlledLight();
            pair.Exclude();

            // When: Reset() is called
            pair.Reset();

            // Then: state stays Excluded
            Assert.Equal(LightControlState.Excluded, pair.AppControlState);
        }

        [Fact]
        public void Exclude_WithPendingReset_ClearsResetOccurred()
        {
            // Given: a light that has a pending Reset (ResetOccurred = true)
            var pair = HueShiftControlledLight();
            pair.Reset();
            Assert.True(pair.ResetOccurred);

            // When: Exclude() is called
            pair.Exclude();

            // Then: ResetOccurred is cleared — so Unexclude() cannot trigger an unexpected brightness-254 jump
            Assert.False(pair.ResetOccurred);
        }
    }
}
