# Manual Override detection based on CT range, not Expected Light divergence

IKEA TRADFRI bulbs connected via the Hue Bridge apply their own power-on CT (454 mired) a
few seconds after being commanded to a different value. The previous model detected Manual
Override by comparing Network Light to Expected Light: any divergence while `PowerState == On`
and `AppControlState == HueShift` was treated as a user override. This caused TRADFRI bulbs to
be permanently locked out of app control on every power-on cycle, requiring a physical toggle
to recover, even though the user had not touched them.

We redefined Manual Override as: a light whose CT mode value falls outside the configured
operating range [Coolest, Warmest] (strict, no tolerance), or whose XY mode state either
cannot be converted to a CT value or converts to one outside that range. CT values within the
range — including a TRADFRI reverting to its 454 mired default — are treated as Drift, not
Manual Override.

We also removed the Sync Grace Period and the `Syncing` power state. The previous model held
lights in `Syncing` between command dispatch and Bridge confirmation, retried periodically, and
promoted to Manual if the grace period expired without confirmation. This was the mechanism
that made TRADFRI reverts fatal (the revert could race the confirmation window). Under the new
model, Sync is fire-and-forget over `BasicTransitionDuration`. Drift (divergence from Expected
Light by more than 10 mired while in-range and `PowerState == On`) is detected on every poll
and triggers an immediate re-Sync — making correction self-healing without any grace period or
explicit confirmation step.

## Considered Options

**Grace-period extension after confirmation.** We considered adding a post-sync grace period —
after a Sync was confirmed, suppress Manual Override detection for N seconds. This would mask
TRADFRI reverts but would still leave the light at the wrong CT until the next Adaptive tick
(up to 60 s), and it left the `Syncing` complexity intact. Rejected in favour of the simpler
CT-range definition.

**Re-sync on any divergence from Expected Light.** We considered keeping Expected Light
comparison as the trigger for both Manual detection and re-sync, but re-syncing instead of
going to Manual when the CT is within range. Rejected because it conflates "the user changed
this light" with "the bulb misbehaved," and makes the Manual Override concept depend on
transient bridge state rather than observable light state.
