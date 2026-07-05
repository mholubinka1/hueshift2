# LightControlPair state machine: three control states with cross-cutting attributes

`LightControlPair` previously tracked control and power as two orthogonal properties:
`LightControlState` (`HueShift`, `Manual`, `Excluded`) and `LightPowerState` (`On`, `Off`,
`Transitioning`). Combining them produced illegal combinations — a light could be
simultaneously `Transitioning` and `Manual` — with no enforcement. Bug reports about
unexpected Manual Override transitions were hard to diagnose because there was no explicit
record of which transition was taken under what conditions.

We collapsed the model into three named control states (`HueShiftControlled`, `ManualOverride`,
`Excluded`) with three cross-cutting boolean attributes (`isOn`, `isTransitioning`,
`isReachable`) that apply across all states. `LightPowerState` is deleted. State transitions
are enforced by private named methods; the public API (`Refresh`, `ExecuteCommand`,
`RequiresSync`, `Reset`) is unchanged. Illegal combinations (e.g. `Transitioning` while in
`ManualOverride`) are unrepresentable because `isTransitioning` is only meaningful in
`HueShiftControlled` — no other state ever calls `BeginTransition()`.

## Considered Options

**Five top-level states** (`HueShiftControlled`, `ManualOverride`, `Excluded`, `Transitioning`,
`Unreachable`) as described in the original feature spec. Rejected because `Transitioning`,
`Unreachable`, `On`, and `Off` are all better modelled as attributes: they apply across
control states (a `ManualOverride` light is also on or off, also reachable or not), and
making them top-level states would require storing a "return state" to restore on exit from
`Transitioning` or `Unreachable`.

**Discriminated union** (sealed class hierarchy). Rejected in favour of an enum with private
guard methods. C# has no native DU; a sealed class hierarchy for five variants with
cross-cutting attributes produces more boilerplate than the logic it replaces, and the
invariants are enforced equally well by private transition methods.

## Consequences

- `isReachable = false` (including `IsReachable == null` from the Bridge) does not change the
  control state; drift and Manual Override checks are suppressed, and `isOn` is preserved at
  its last known value. Reconnecting to `HueShiftControlled` sets `SyncRequired = true`.
- `ManualOverride → HueShiftControlled` fires only on a genuine power cycle (`isOn` was false,
  bridge now reports on) — going unreachable does not trigger this transition.
- `Transition` is fully private; callers use `IsTransitioning()` and
  `TransitionSecondsRemaining()` query methods. `ExecuteInstantaneousCommand` is renamed
  `UpdateExpectedState()` and remains callable on any control state.
