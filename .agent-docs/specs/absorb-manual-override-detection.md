# Absorb Manual Override and Drift Detection into LightControlPair

## Problem Statement

Manual Override detection (ADR-0001: CT-range model) and Drift detection live in `Helpers/ExtensionMethods.cs` as extension methods on `Q42.State`, a 3rd-party type. This scatters the Light Control Pair state machine logic across two classes with no meaningful seam between them. A developer investigating a Manual Override bug must read `ExtensionMethods`, `LightControlPair`, and `LightRegistry` to trace the CT-bounds chain. Tests that exercise override detection must construct `Q42.State` objects even though the domain decision belongs to `LightControlPair`.

A dead `Equals(this State, AppLightState, int, int)` overload in the same file is a leftover from the pre-ADR-0001 equality-based detection model and adds noise.

## Solution

Move `IsManualOverride` and `HasDrifted` into `LightControlPair` as private methods. The state machine decision and its implementation are co-located; Manual Override bugs are catchable from a single class. Delete the dead `Equals(this State, ...)` overload. `TryXyToCt` stays in `ExtensionMethods` (still needed by `Filter()`); the new private methods call it directly.

## User Stories

1. As a developer investigating a Manual Override bug, I want the detection logic to live inside `LightControlPair`, so that I only need to open one file.
2. As a developer writing a test for Manual Override, I want to exercise the CT-range and XY-conversion logic through `LightControlPair.Refresh()`, so that I do not need to construct `Q42.State` objects or reference `ExtensionMethods` directly.
3. As a developer reading `ExtensionMethods`, I want it to contain only utilities (`TryXyToCt`, `Filter`, `ToCommand`, `Trim`, `Reset`, `Clamp`) and no domain state-machine decisions, so that its scope is clear.

## Implementation Decisions

- Add `private bool IsManualOverride(int minCt, int maxCt)` to `LightControlPair`. The method reads `this.NetworkLight` and calls `ExtensionMethods.TryXyToCt(...)` for XY coordinates. Logic is identical to the current extension method.
- Add `private bool HasDrifted()` to `LightControlPair`. The method reads `this.NetworkLight` and `this.ExpectedLight` and calls `ExtensionMethods.TryXyToCt(...)` for XY coordinates. Logic is identical to the current extension method.
- Update `LightControlPair.Refresh()` call sites: `this.NetworkLight.IsManualOverride(minCt, maxCt)` → `IsManualOverride(minCt, maxCt)` and `this.NetworkLight.HasDrifted(this.ExpectedLight)` → `HasDrifted()`.
- Delete from `ExtensionMethods`: `IsManualOverride(this State, int, int)`, `HasDrifted(this State, AppLightState)`, the `#region Manual Override and Drift Detection` block, and the dead `Equals(this State, AppLightState, int, int)` overload with its `#region Light Equality` block.
- `TryXyToCt` stays in `ExtensionMethods` — it is still the implementation of XY→CT Conversion used by `Filter()`. `LightControlPair` calls it as `ExtensionMethods.TryXyToCt(...)` via the existing `using HueShift2.Helpers` import (which remains because `ToCommand()` also lives there).
- No interface changes. No schema changes. No new public surface.

## Testing Decisions

- The single test seam is `LightControlPair.Refresh()`. All existing `LightControlPairManualOverrideTests` exercise the behavior through this seam and pass unchanged — the refactor moves logic but does not change behavior.
- No new tests are required. This is a locality refactor: the observable contract of `LightControlPair` is unchanged.
- Prior art: `HueShift2.Tests/Control/LightControlPairManualOverrideTests.cs` — 13 scenarios covering CT-range boundaries, XY conversion, drift tolerance, and transition settling period suppression.

## Out of Scope

- Moving `Filter()`, `ToCommand()`, or `TryXyToCt` out of `ExtensionMethods` (Architecture Candidates B–E).
- Making `AppLightState` an immutable value object (Architecture Candidate B).
- Any change to `Q42.State` usage in `LightRegistry` or `LightController`.
- Any new Manual Override or Drift test scenarios (existing coverage is sufficient).

## Further Notes

ADR-0001 defines Manual Override detection as: CT mode value outside `[Coolest, Warmest]` (strict), or XY mode that cannot be converted or converts out of range, or any non-CT/XY colour mode. The private methods must preserve this definition exactly. The 10-mired Drift tolerance is also preserved as-is.
