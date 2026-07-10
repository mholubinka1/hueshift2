> Merged and closed.

# Issues: p2-7-solar-event-provider

## Add `ISolarEventProvider` interface and `SolarEventProvider` implementation (#380)

**Blocked by**: None

**User stories**: 1, 2, 3

### What to build

New interface `ISolarEventProvider { AdaptiveSolarEvents GetEventsForDate(DateOnly date); }` and `SolarEventProvider` implementation. Move `DetermineTimeZoneId()`, `new SolarTimes(...)`, UTC conversion, clamping to `SolarTransitionTimeLimits`, and the Sunrise/Sunset date invariant check out of `AdaptiveScheduleProvider` and into `SolarEventProvider`. Register in DI. `AdaptiveScheduleProvider` still compiles (wiring in the follow-up issue).

### Acceptance criteria

- [x] `ISolarEventProvider` interface exists with `GetEventsForDate(DateOnly date)` method
- [x] `SolarEventProvider` implements `ISolarEventProvider`
- [x] `SolarEventProvider` owns all timezone resolution and solar calculation logic
- [x] Sunrise and sunset are clamped to `SolarTransitionTimeLimits` from config
- [x] Sunrise.Date == Sunset.Date invariant check is preserved
- [x] `SolarEventProvider` is registered in DI
- [x] `AdaptiveScheduleProvider` still compiles

---

## Wire `ISolarEventProvider` into `AdaptiveScheduleProvider` (#386)

**Blocked by**: #380

**User stories**: 1, 2, 3

### What to build

Inject `ISolarEventProvider` into `AdaptiveScheduleProvider` and remove `DetermineTimeZoneId()` and `RefreshSolarEvents()`. In `TransitionRequired()`, replace the `RefreshSolarEvents(currentTime)` call with `solarEvents = _solarEventProvider.GetEventsForDate(DateOnly.FromDateTime(currentTime))`, still guarded by `RefreshRequired()`. `RefreshRequired()` and the `solarEvents` cache field remain.

### Acceptance criteria

- [x] `AdaptiveScheduleProvider` constructor takes `ISolarEventProvider`
- [x] `DetermineTimeZoneId()` and `RefreshSolarEvents()` removed from `AdaptiveScheduleProvider`
- [x] `TransitionRequired()` calls `ISolarEventProvider.GetEventsForDate()` when refresh is required
- [x] `RefreshRequired()` logic unchanged
- [x] All existing tests pass
- [x] `AdaptiveScheduleProvider` has no direct dependency on `SolarTimes`, `TZConvert`, or `RuntimeInformation`

---
