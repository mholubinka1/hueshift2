using HueShift2.Control;
using HueShift2.Model;
using Q42.HueApi;
using System;
using Xunit;

namespace HueShift2.Tests.Control
{
    public class LightControlPairSyncGraceTests
    {
        private static readonly TimeSpan SyncDuration = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan SyncGracePeriod = TimeSpan.FromSeconds(10);
        private const int Coolest = 250;
        private const int Warmest = 454;

        private static LightControlPair CreateLightOn(int colourTemperature = 339, byte brightness = 254)
        {
            var networkLight = new Light
            {
                Id = "1",
                Name = "Test Light",
                State = new State
                {
                    On = true,
                    Brightness = brightness,
                    ColorMode = "ct",
                    ColorTemperature = colourTemperature,
                    IsReachable = true,
                },
            };
            return new LightControlPair(networkLight);
        }

        private static State BridgeState(int colourTemperature, byte brightness = 254, bool on = true)
        {
            return new State
            {
                On = on,
                Brightness = brightness,
                ColorMode = "ct",
                ColorTemperature = colourTemperature,
                IsReachable = true,
            };
        }

        [Fact]
        public void BridgeConfirmsSyncWithinGracePeriod_PromotesToOnAndReportsConfirmed()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var syncIssuedAt = DateTime.UtcNow;
            pair.ExecuteSync(SyncDuration, syncIssuedAt);

            pair.Refresh(BridgeState(339), syncIssuedAt.AddSeconds(1), Coolest, Warmest, SyncGracePeriod);

            Assert.Equal(LightPowerState.On, pair.PowerState);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
            Assert.True(pair.SyncConfirmed);
            Assert.False(pair.SyncFailed);
        }

        [Fact]
        public void BridgeNotYetConfirmedAndStillWithinGracePeriod_StaysSyncingAndRequestsRetry()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var syncIssuedAt = DateTime.UtcNow;
            pair.ExecuteSync(SyncDuration, syncIssuedAt);

            // Bridge reports a stale CT outside the ±50 mired match window; the 2s sync
            // transition timer has expired, but we're still well within the 10s grace period.
            pair.Refresh(BridgeState(454), syncIssuedAt.AddSeconds(3), Coolest, Warmest, SyncGracePeriod);

            Assert.Equal(LightPowerState.Syncing, pair.PowerState);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
            Assert.True(pair.SyncRequired);
            Assert.False(pair.SyncConfirmed);
            Assert.False(pair.SyncFailed);
        }

        [Fact]
        public void BridgeStillNotConfirmedAfterGracePeriodElapses_ShiftsToManualAndReportsFailed()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var syncIssuedAt = DateTime.UtcNow;
            pair.ExecuteSync(SyncDuration, syncIssuedAt);

            // 11s after the sync was first issued: past the 10s grace period, and the bridge
            // still reports a CT that is not within ±50 mired of the commanded 339.
            pair.Refresh(BridgeState(454), syncIssuedAt.AddSeconds(11), Coolest, Warmest, SyncGracePeriod);

            Assert.Equal(LightPowerState.On, pair.PowerState);
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
            Assert.False(pair.SyncRequired);
            Assert.False(pair.SyncConfirmed);
            Assert.True(pair.SyncFailed);
        }

        [Fact]
        public void UserChangesStableLightDirectly_ShiftsToManualWithoutAffectingSyncOutcome()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var now = DateTime.UtcNow;

            // Light is already stable On (no sync in progress) when the user changes it directly.
            pair.Refresh(BridgeState(339), now, Coolest, Warmest, SyncGracePeriod);
            pair.Refresh(BridgeState(454), now.AddSeconds(5), Coolest, Warmest, SyncGracePeriod);

            Assert.Equal(LightPowerState.On, pair.PowerState);
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
            Assert.False(pair.SyncConfirmed);
            Assert.False(pair.SyncFailed);
        }

        [Fact]
        public void LightTurnsOffMidGracePeriod_ThenBackOn_GetsAFreshFullGracePeriod()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var syncIssuedAt = DateTime.UtcNow;
            pair.ExecuteSync(SyncDuration, syncIssuedAt);

            // Still mid-grace, bridge unconfirmed, then the light is switched off.
            pair.Refresh(BridgeState(454), syncIssuedAt.AddSeconds(3), Coolest, Warmest, SyncGracePeriod);
            pair.Refresh(BridgeState(454, on: false), syncIssuedAt.AddSeconds(4), Coolest, Warmest, SyncGracePeriod);
            Assert.False(pair.SyncRequired);

            // Powers back on and a new sync is issued, 11s after the *original* sync — which
            // would already be outside the old 10s grace window if it weren't reset. The bridge
            // still hasn't confirmed and the 2s sync transition has expired, but only 9s have
            // elapsed since the *new* sync was issued, so this must be a retry, not a failure.
            var secondSyncIssuedAt = syncIssuedAt.AddSeconds(11);
            pair.ExecuteSync(SyncDuration, secondSyncIssuedAt);
            pair.Refresh(BridgeState(454, on: true), secondSyncIssuedAt.AddSeconds(9), Coolest, Warmest, SyncGracePeriod);

            Assert.Equal(LightPowerState.Syncing, pair.PowerState);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
            Assert.True(pair.SyncRequired);
            Assert.False(pair.SyncConfirmed);
            Assert.False(pair.SyncFailed);
        }

        [Fact]
        public void ZeroGracePeriodConfigured_FailsImmediatelyOnFirstUnconfirmedCheck()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var syncIssuedAt = DateTime.UtcNow;
            pair.ExecuteSync(SyncDuration, syncIssuedAt);

            // Sync transition timer has just expired; with a 0s grace period there is no
            // retry window at all, so this must fail on the very first unconfirmed check.
            pair.Refresh(BridgeState(454), syncIssuedAt.AddSeconds(2), Coolest, Warmest, TimeSpan.Zero);

            Assert.Equal(LightPowerState.On, pair.PowerState);
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
            Assert.False(pair.SyncRequired);
            Assert.False(pair.SyncConfirmed);
            Assert.True(pair.SyncFailed);
        }

        [Fact]
        public void GraceExpiresAfterRetryWasPending_RetryClearedAndSyncFailed()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var syncIssuedAt = DateTime.UtcNow;
            pair.ExecuteSync(SyncDuration, syncIssuedAt);

            // First poll: transition expired, bridge unconfirmed, within grace → SyncRequired = true.
            pair.Refresh(BridgeState(454), syncIssuedAt.AddSeconds(3), Coolest, Warmest, SyncGracePeriod);
            Assert.True(pair.SyncRequired);

            // Second poll: grace period has now elapsed, bridge still unconfirmed → sync failed.
            pair.Refresh(BridgeState(454), syncIssuedAt.AddSeconds(11), Coolest, Warmest, SyncGracePeriod);

            Assert.Equal(LightPowerState.On, pair.PowerState);
            Assert.Equal(LightControlState.Manual, pair.AppControlState);
            Assert.True(pair.SyncFailed);
            Assert.False(pair.SyncRequired);
            Assert.False(pair.SyncConfirmed);
        }

        [Fact]
        public void BridgeConfirmsSyncAfterRetryWasPending_RetryClearedAndSyncConfirmed()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var syncIssuedAt = DateTime.UtcNow;
            pair.ExecuteSync(SyncDuration, syncIssuedAt);

            // First poll: transition expired, bridge unconfirmed, within grace → SyncRequired = true.
            pair.Refresh(BridgeState(454), syncIssuedAt.AddSeconds(3), Coolest, Warmest, SyncGracePeriod);
            Assert.True(pair.SyncRequired);

            // Second poll: bridge now reports the commanded CT → sync confirmed.
            pair.Refresh(BridgeState(339), syncIssuedAt.AddSeconds(4), Coolest, Warmest, SyncGracePeriod);

            Assert.Equal(LightPowerState.On, pair.PowerState);
            Assert.Equal(LightControlState.HueShift, pair.AppControlState);
            Assert.True(pair.SyncConfirmed);
            Assert.False(pair.SyncRequired);
            Assert.False(pair.SyncFailed);
        }

        [Fact]
        public void WhileRetryingMidGrace_RequiresRetrySyncYieldsCommandAndClearsFlag()
        {
            var pair = CreateLightOn(colourTemperature: 339);
            var syncIssuedAt = DateTime.UtcNow;
            pair.ExecuteSync(SyncDuration, syncIssuedAt);
            pair.Refresh(BridgeState(454), syncIssuedAt.AddSeconds(3), Coolest, Warmest, SyncGracePeriod);

            var requiresRetry = pair.RequiresRetrySync(out var retryCommand);

            Assert.True(requiresRetry);
            Assert.NotNull(retryCommand);
            Assert.Equal(339, retryCommand!.ColorTemperature);
            Assert.False(pair.RequiresRetrySync(out _));
        }
    }
}
