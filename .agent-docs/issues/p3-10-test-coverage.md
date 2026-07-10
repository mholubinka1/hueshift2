> Work complete — PR ready to merge.

# Issues: p3-10-test-coverage

## Add `AdaptiveLightColourCalculatorTests` (#385)

**Blocked by**: None

**User stories**: 1, 2

### What to build

New `AdaptiveLightColourCalculatorTests` in `HueShift2.Tests/Control/`. Construct the calculator directly with fixed config values — no mocks. Cover all 7 sun position boundary cases (before sunrise, at sunrise, between sunrise and noon, at solar noon, between noon and sunset, at sunset, after sunset) and all 3 CT mapping cases (sun position 1.0 → Coolest, -1.0 → Warmest, 0.0 → Warmest).

### Acceptance criteria

- [x] `AdaptiveLightColourCalculatorTests` class exists in `HueShift2.Tests/Control/`
- [x] All 7 sun position boundary cases are tested
- [x] All 3 CT mapping cases are tested
- [x] Tests use fixed `AdaptiveSolarEvents` and fixed `ColourTemperature` config values
- [x] All tests pass with no network, filesystem, or Bridge access

---

## Add `AdaptiveScheduleProviderTests` (#387)

**Blocked by**: #386

**User stories**: 3

### What to build

New `AdaptiveScheduleProviderTests` in `HueShift2.Tests/Control/` using a fake `ISolarEventProvider`. Test `TransitionRequired()` for all 5 transition type cases (`FirstRun`, `Solar` at sunrise, `Solar` at sunset, `Adaptive`, `Null`) and the once-per-day refresh cadence (refresh on date change; no re-fetch same day). No real solar library, timezone resolver, or network access.

### Acceptance criteria

- [ ] `AdaptiveScheduleProviderTests` class exists in `HueShift2.Tests/Control/`
- [ ] All 5 `TransitionType` cases tested
- [ ] Once-per-day refresh cadence tested (refresh on date change, no re-fetch same day)
- [ ] Tests use a fake `ISolarEventProvider` — no real `SolarTimes` library invoked
- [ ] All tests pass with no network, filesystem, or Bridge access

---
