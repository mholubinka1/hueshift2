# Remaining Test Coverage: Colour Calculator and Schedule Provider

## Problem Statement

The `HueShift2.Tests` project exists and covers `LightControlPair` (manual override, transitioning, unreachable, exclusion) and `LightRegistry`. Two high-risk pure-logic modules remain untested:

1. `AdaptiveLightColourCalculator` — sun position interpolation and colour temperature calculation. Pure functions with no dependencies; regressions here silently produce wrong colours.
2. `AdaptiveScheduleProvider.TransitionRequired` — determines when and which type of transition fires. This requires the `ISolarEventProvider` seam from P2-7 before it can be tested without a real solar library.

## Solution

Add two new test classes to `HueShift2.Tests`. The colour calculator tests have no prerequisites. The schedule provider tests are blocked on P2-7.

## User Stories

1. As a developer, I want tests covering sun position boundary values (before sunrise, at sunrise, at noon, at sunset, after sunset) so that regressions in the colour interpolation algorithm are caught immediately.
2. As a developer, I want tests covering colour temperature mapping at sun position extremes so that `Coolest` and `Warmest` config values are verified end-to-end.
3. As a developer, I want tests for each `TransitionType` (`FirstRun`, `Solar`, `Adaptive`, `Null`) so that scheduling regressions are caught without a live Bridge or real solar calculator.

## Implementation Decisions

### `AdaptiveLightColourCalculatorTests`

New test class in `HueShift2.Tests/Control/`. Tests `AdaptiveLightColourCalculator` directly — it is a pure function class with no injected dependencies (only a `HueShiftOptions` or similar config value). Test cases:

- Sun position before sunrise → -1.0 (or equivalent minimum)
- Sun position at sunrise → 0.0
- Sun position between sunrise and noon → value in (0.0, 1.0)
- Sun position at solar noon → 1.0
- Sun position between noon and sunset → value in (0.0, 1.0)
- Sun position at sunset → 0.0
- Sun position after sunset → -1.0 (or equivalent minimum)
- CT at sun position 1.0 → `Coolest` configured value
- CT at sun position -1.0 → `Warmest` configured value
- CT at sun position 0.0 → `Warmest` configured value

No mocks or fakes required — construct the calculator with fixed config values.

### `AdaptiveScheduleProviderTests`

New test class in `HueShift2.Tests/Control/`. **Blocked on P2-7** (requires `ISolarEventProvider` seam). Once P2-7 is in place, inject a fake `ISolarEventProvider` returning fixed `AdaptiveSolarEvents` for a given date. Test cases:

- `lastRunTime == null` → `TransitionType.FirstRun`
- `lastRunTime` before sunrise, `currentTime` after sunrise → `TransitionType.Solar`
- `lastRunTime` before sunset, `currentTime` after sunset → `TransitionType.Solar`
- `lastTransitionTime` more than `TransitionInterval` ago, no solar crossing → `TransitionType.Adaptive`
- `lastTransitionTime` less than `TransitionInterval` ago, no solar crossing → `TransitionType.Null`
- Solar events refreshed when date changes (fake called with today's date after midnight)
- Solar events not re-fetched when date has not changed (fake called exactly once per day)

### No new test infrastructure required

Both test classes follow the existing pattern in `HueShift2.Tests/Control/`. No new base classes, fixtures, or utilities needed.

## Testing Decisions

These tests test behaviour via the public API of each class, not internal state. Constructor injection is the test seam for `AdaptiveScheduleProvider`; `AdaptiveLightColourCalculator` requires no injection. All tests run with no network, filesystem, or Bridge access.

## Out of Scope

- Integration tests that involve the full scheduling loop end-to-end.
- Tests for `LightSynchroniser`, `LightController`, or `LightRegistry` (existing coverage is sufficient).
- Contract tests against the real IpStack or Hue APIs.

## Further Notes

`AdaptiveScheduleProviderTests` has a hard dependency on P2-7. The `AdaptiveLightColourCalculatorTests` work can begin immediately. Implement them as two separate issues with an explicit blocked-by relationship.
