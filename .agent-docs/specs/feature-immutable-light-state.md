# Immutable Light State

## Problem Statement

`AppLightState` and `Colour` are mutable classes. `LightControlPair` mutates them in-place across multiple methods: `Refresh()` updates Expected Light Brightness before the Manual Override check, `UpdateExpectedState()` sets Brightness from either the command or the Network Light, and `ChangeColour()` first zeroes out all Colour fields via `ClearColourState()` then sets the new values. This two-step clear-then-set pattern leaves Expected Light Colour in an invalid intermediate state — Mode `None` with all coordinates null — between the two calls. A future reader or collaborator is one extraction away from observing that window as a bug.

Additionally, `Colour.Equals(object obj)` contains a pre-existing defect: it casts to `AppLightState` instead of `Colour`, meaning cross-type comparisons via the object overload always return false. Several `Equals(Colour)` branches throw `NotImplementedException`. These bugs are invisible in production today but become misleading once the type is presented as a value object.

## Solution

Convert `AppLightState` and `Colour` to classes with `init`-only properties. All mutation sites in `LightControlPair` produce new instances atomically via object initializers — there is no intermediate state. `DeepClone()` is deleted from both types (immutable classes need no cloning). `ClearColourState()` disappears, absorbed into a single Colour construction inside `ChangeColour()`. The `Colour.Equals` defects are corrected at the same time as the equality surface is rewritten.

Note: `record class` was considered but rejected due to a compiler constraint — the project does not enable nullable reference types globally, which causes CS0111 when providing a custom `Equals(T? other)` override alongside the synthesized record method. Init-only classes with explicit object-initializer construction achieve the same functional goals (atomicity, no mutation after construction, no cloning required) without the compiler conflict.

## User Stories

1. As a developer reading `LightControlPair`, I want Expected Light to be updated atomically, so that I never see a partially-constructed Expected Light Colour while stepping through the code.
2. As a developer reading `AppLightState` or `Colour`, I want `init`-only properties with no `DeepClone()` method, so that the types signal immutability at their interface.
3. As a developer writing a test that compares two `Colour` instances with the same XY coordinates, I want value equality to hold, so that I do not need to write custom comparison code.
4. As a developer calling `Colour.Equals(object)`, I want the method to behave correctly for `Colour`-to-`Colour` comparisons, so that boxing a `Colour` does not silently break equality.

## Implementation Decisions

- Convert `AppLightState` to a class with `init`-only properties (`Brightness`, `Colour`). Add internal `WithBrightness(byte?)` and `WithColour(Colour)` factory methods to express atomic updates without repetition.
- Convert `Colour` to a class with `init`-only properties (all five: `Mode`, `ColourCoordinates`, `ColourTemperature`, `Hue`, `Saturation`).
- `ColourCoordinates` remains `double[]`. Copy the array defensively on construction (`ColourCoordinates = colourCoordinates.DeepClone()`) to prevent callers from mutating the stored array. Override `Equals(Colour other)` to use `ExtensionMethods.ArrayEquals` for the coordinates field. Override `GetHashCode()` consistently. `ColourMode.None` and `ColourMode.Other` compare by `Hue` and `Saturation`.
- Fix `Colour.Equals(object obj)`: cast to `Colour`, not `AppLightState`.
- Fix all `NotImplementedException` branches in `Colour.Equals(Colour other)`.
- Remove `IDeepCloneable<T>` from all types (`AppLightState`, `Colour`, `LightProperties`, `Transition`) and delete their `DeepClone()` methods. Delete `IDeepCloneable.cs`. `LightProperties` holds only immutable `string` fields; direct assignment is safe. `Transition.DeepClone()` had no callers.
- Update `CachedControlPair`: replace both `light.ExpectedLight.DeepClone()` and `light.Properties.DeepClone()` with direct assignment.
- Rewrite all `LightControlPair` mutation sites using `WithBrightness`/`WithColour` and explicit `new Colour(...)` construction:
  - `Refresh()`: `this.ExpectedLight = this.ExpectedLight.WithBrightness(this.NetworkLight.Brightness)`
  - `UpdateExpectedState()`: `this.ExpectedLight = this.ExpectedLight.WithBrightness(brightness)` then `ChangeColour(command)`
  - `ChangeColour()`: `this.ExpectedLight = this.ExpectedLight.WithColour(new Colour(...))` per colour mode; `ClearColourState()` is deleted.
- No public interface, schema, or configuration changes.

## Testing Decisions

- New `ColourEqualityTests` class covering: two `Colour` instances with identical XY coordinates compare equal; two with different XY compare unequal; CT-only instances compare equal by value; `Equals(object)` with a boxed `Colour` returns the correct result; null `ColourCoordinates` handling.
- All 86 existing tests pass unchanged. `LightControlPairManualOverrideTests` exercises the refactored `Refresh()` path through the same public seam — the refactor is behaviour-neutral.
- Prior art: `HueShift2.Tests/Control/LightControlPairManualOverrideTests.cs`.

## Out of Scope

- Converting `LightProperties` or `Transition` to records.
- Changing `ColourCoordinates` from `double[]` to `ImmutableArray<double>` or any other type.
- Any change to `TryXyToCt`, `ArrayEquals`, or other `ExtensionMethods` utilities beyond what is needed to support the new `Colour.Equals` implementation.
- New Manual Override or Drift test scenarios.

## Further Notes

The partial-update hazard has not caused a known production bug. This is a pre-emptive correctness improvement: the two-step clear-then-set pattern in `ChangeColour()` is the kind of code that is safe today and dangerous the moment someone adds a read between the two calls. Converting to records eliminates the hazard structurally rather than by convention.

`ClearColourState()` exists solely to reset Colour before `ChangeColour()` sets new values. It has no callers outside `ChangeColour()` and no reason to exist once `ChangeColour()` constructs a fresh `Colour` in one step. Delete it.
