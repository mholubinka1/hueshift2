# Extract Solar Event Provider

## Problem Statement

`AdaptiveScheduleProvider` owns three unrelated concerns: resolving the OS timezone, calling the solar calculator library, and deciding when transitions are due. Testing whether the app correctly triggers a Solar Transition at sunset in Helsinki in December requires wiring up a real timezone resolver and solar calculator — the very things you want to control in tests. Any change to how solar times are computed touches the same class as scheduling logic.

## Solution

Extract timezone resolution and solar calculation into a dedicated `SolarEventProvider` behind `ISolarEventProvider`. `AdaptiveScheduleProvider` injects this interface and calls it instead of computing solar events itself. Tests can inject a fake that returns fixed events for a given date, making scheduling logic testable without any system clock or solar library dependency.

## User Stories

1. As a developer, I want to test that `AdaptiveScheduleProvider` triggers a Solar Transition at sunset without wiring up a real solar calculator, so that tests are fast and deterministic.
2. As a developer, I want to test the sunrise/sunset clamping behaviour in isolation, so that I can verify polar-latitude edge cases without a scheduling context.
3. As a developer, I want the OS timezone resolution logic (Windows vs Linux branching) to live in one class, so that it is easy to find and modify.

## Implementation Decisions

### `ISolarEventProvider`

New interface with a single method:

```csharp
interface ISolarEventProvider {
    AdaptiveSolarEvents GetEventsForDate(DateOnly date);
}
```

### `SolarEventProvider`

New class implementing `ISolarEventProvider`. Owns everything currently in `AdaptiveScheduleProvider` that relates to computing solar events:

- `DetermineTimeZoneId(string timeZone)` — Windows/Linux branching via `RuntimeInformation.IsOSPlatform`, `TZConvert.IanaToWindows`, and `TimeZoneInfo.FindSystemTimeZoneById`. The `PlatformNotSupportedException` (OSX) remains here.
- `new SolarTimes(date, latitude, longitude)` construction.
- UTC → local time conversion via `TimeZoneInfo.ConvertTimeFromUtc`.
- Clamping to `SolarTransitionTimeLimits` from `HueShiftOptions`.
- The `solarEvents.Sunrise.Date != solarEvents.Sunset.Date` invariant check.

`SolarEventProvider` injects `IOptionsMonitor<HueShiftOptions>` and `ILogger<SolarEventProvider>`.

### `AdaptiveScheduleProvider`

- Inject `ISolarEventProvider` via constructor; remove `DetermineTimeZoneId` and `RefreshSolarEvents` methods.
- Replace the `RefreshSolarEvents(currentTime)` call in `TransitionRequired` with `solarEvents = solarEventProvider.GetEventsForDate(DateOnly.FromDateTime(currentTime))`.
- `RefreshRequired()` stays in `AdaptiveScheduleProvider` — it controls the once-per-day refresh cadence, which is scheduling concern, not solar calculation.
- The `solarEvents` field remains in `AdaptiveScheduleProvider` as a cache (null until first call).

### DI Registration

Register `SolarEventProvider` as the implementation of `ISolarEventProvider` in `Startup.cs` / `Program.cs`.

## Testing Decisions

**`SolarEventProvider` tests** — test in isolation with known lat/long/timezone inputs. Verify clamping to `SunriseLower`, `SunriseUpper`, `SunsetLower`, `SunsetUpper`. No scheduling logic involved; use real `SolarTimes` library with controlled inputs.

**`AdaptiveScheduleProvider` tests** — inject a fake `ISolarEventProvider` returning fixed `AdaptiveSolarEvents`. All existing scheduling scenarios (FirstRun, Solar, Adaptive, Null) become testable without any real timezone or solar dependency. Prior art: `LightControllerRefreshTests` pattern with substituted interfaces.

Test seam: `ISolarEventProvider` is the single seam — one interface, clean break between solar calculation and scheduling logic.

## Out of Scope

- Caching solar events across process restarts.
- Supporting more than one solar event provider (e.g. a different library).
- Changing the clamping algorithm or the transition-refresh cadence.
- macOS support (the existing `PlatformNotSupportedException` is preserved).
