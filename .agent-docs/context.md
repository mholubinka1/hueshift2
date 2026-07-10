# HueShift

An adaptive Philips Hue light controller that adjusts colour temperature and brightness throughout the day based on solar position, automatically recovering from manual user overrides.

## Language

### Lights and State

**Light**:
A single Philips Hue smart bulb, identified by a Bridge-assigned string ID.
_Avoid_: Bulb, device, lamp

**Bridge**:
The Philips Hue hub on the local network that mediates all light commands and state queries. One Bridge per HueShift instance.
_Avoid_: Hub, gateway, controller

**Light Control Pair**:
The runtime pairing of a Light's network state, expected state, control state, and power/transition attributes. The unit of management in the Light Registry.
_Avoid_: Light wrapper, light entry, light model

**Network Light**:
The raw state of a Light as last reported by the Bridge — brightness, colour mode, and colour values.
_Avoid_: Current state, Bridge state, actual state

**Expected Light**:
The target state the app intends a Light to be in. Used for Drift detection and as the Sync target.
_Avoid_: Desired state, target state, commanded state

**Light Properties**:
Immutable metadata about a Light: ID, Name, ModelId, ProductId. Fetched from the Bridge at discovery.
_Avoid_: Light info, light metadata

### Control State Machine

**Light Control State**:
Three-state enum defining app ownership of a Light: `HueShiftControlled`, `Manual`, `Excluded`.
_Avoid_: App state, ownership state

**HueShiftControlled**:
The state in which the app owns the light and issues Adaptive and Sync commands to it.
_Avoid_: Controlled, active, managed

**Manual**:
The state in which the user has taken the light out of the app's CT range; the app backs off until the light power-cycles.
_Avoid_: Override state, user-controlled, manual override (as a state name)

**Excluded**:
The state in which a light is permanently ignored by HueShift, applied via the `LightsToExclude` config. Live — no restart required.
_Avoid_: Disabled, ignored, skipped

**Light Control Attributes**:
Three cross-cutting booleans that apply across all Light Control States: `isOn`, `isTransitioning`, `isReachable`.
_Avoid_: Light flags, status flags

**isOn**:
Bridge reports the light is currently on. `Manual → HueShiftControlled` fires only on a genuine power cycle: `isOn` was false, Bridge now reports on, and the light was previously reachable.
_Avoid_: Power state, powered

**isTransitioning**:
The light is mid-way through a commanded transition, including the Transition Settling Period. Drift and Manual Override checks are suppressed while true.
_Avoid_: In transition, transitioning state

**isReachable**:
The Bridge can communicate with the light. When false, divergence checks are suppressed and `isOn` is preserved at its last known value. Reconnection sets `SyncRequired = true`.
_Avoid_: Connected, online, available

### Manual Override and Drift

**Manual Override**:
The condition where a HueShift-controlled light is on and its CT is outside the configured [Coolest, Warmest] range, or its colour mode is XY (where XY cannot be converted to an in-range CT value), or its colour mode is HS or unknown. Clears on power cycle or on a Solar/FirstRun Reset.
_Avoid_: User override, light override

**Drift**:
The condition where a HueShift-controlled light is on and its Network Light diverges from Expected Light by more than 10 mired, while the CT remains within [Coolest, Warmest]. Detected every poll; triggers an immediate Sync.
_Avoid_: Divergence, out-of-sync

**Reset**:
Clears Manual Override on all lights and sets `SyncRequired`, triggered on FirstRun and Solar transitions. Ensures lights return to app control after sunrise/sunset.
_Avoid_: Clear override, force sync

### Colour Temperature

**Colour Temperature**:
The warmth of a light, measured in Mired (reciprocal megakelvin). Lower mired = cooler/bluer; higher mired = warmer/more amber.
_Avoid_: CT (in isolation — spell out where ambiguous), colour warmth

**Coolest**:
The upper (coolest/bluest) bound of the operating CT range, in Mired. Typically ~250 mired (~4000 K). CT values at or below Coolest are considered in-range.
_Avoid_: Min CT, cool temperature, daylight

**Warmest**:
The lower (warmest/most amber) bound of the operating CT range, in Mired. Typically ~454 mired (~2200 K). CT values at or above Warmest are considered in-range.
_Avoid_: Max CT, warm temperature, candlelight

**Colour Mode**:
How a light's colour state is represented: `CT` (mired integer), `XY` (CIE 1931 coordinates), `HS` (hue/saturation, unsupported), `None`, or `Other`.
_Avoid_: Colour type, light mode

**XY→CT Conversion**:
Algorithm that converts CIE 1931 XY coordinates to a Mired CT value for drift and Manual Override checks. Returns null when the colour is saturated (outside the white-point locus).
_Avoid_: XY conversion, colour conversion

**Brightness**:
Byte value 0–254 (the Bridge caps at 254; 255 is treated as 254). During the Sleep window, set to NightBrightnessPercentage% of 254.
_Avoid_: Luminance, intensity, level

### Solar Events and Timing

**Solar Events**:
Three reference times computed daily for the configured Geolocation: Sunrise, Solar Noon, and Sunset. Recalculated once per day.
_Avoid_: Sun times, daylight events

**Solar Noon**:
The moment of peak sun elevation; Sun Position = 1.0.
_Avoid_: Midday, noon

**Sunrise / Sunset**:
Solar event times clamped to the configured Solar Transition Time Limits (to handle polar latitudes). Sun Position = 0.0 at these moments.
_Avoid_: Dawn, dusk

**Solar Transition Time Limits**:
Configured time windows clamping computed Sunrise and Sunset times: `SunriseLower`, `SunriseUpper`, `SunsetLower`, `SunsetUpper`.
_Avoid_: Sunrise limits, sunset clamps

**Sun Position**:
A normalised value in [-1.0, 1.0] representing the current time relative to Solar Events. 1.0 at Solar Noon, 0.0 at Sunrise/Sunset, -1.0 at deepest night. Used to interpolate Colour Temperature via quadratic scaling.
_Avoid_: Sun angle, elevation, solar factor

**Sleep Window**:
The period from the configured Sleep time until next Sunrise. During this window, brightness is set to NightBrightnessPercentage and no Adaptive Transitions update colour.
_Avoid_: Night mode, dim mode, night window

**Geolocation**:
Latitude, longitude, and IANA timezone string used to calculate Solar Events. Resolved automatically via the IpStack API on first run.
_Avoid_: Location, GPS, coordinates

### Transitions

**Transition Type**:
The reason a commanded transition was triggered: `Null` (none due this tick), `Sync` (drift correction), `FirstRun` (app startup), `Solar` (sunrise/sunset threshold crossed), `Adaptive` (scheduled interval elapsed).
_Avoid_: Transition kind (in prose — use Transition Type)

**Adaptive Transition**:
A scheduled colour temperature update issued every TransitionInterval seconds, tracking Sun Position continuously.
_Avoid_: Regular transition, periodic update, scheduled transition

**Solar Transition**:
A transition triggered when a Sunrise or Sunset threshold is crossed; also triggers a Reset, clearing Manual Override on all lights.
_Avoid_: Sunrise transition, sunset transition (use Solar Transition for both)

**FirstRun Transition**:
The transition issued when the app starts; discovers all non-excluded lights and applies the cached command to lights that are on.
_Avoid_: Startup transition, initial sync

**Sync**:
A fire-and-forget correction command over `BasicTransitionDuration`, issued when Drift is detected. Self-healing — no retry or confirmation is awaited.
_Avoid_: Re-sync, correction command, force update

**Transition Settling Period**:
A fixed 10-second window after any commanded transition during which Drift and Manual Override checks are suppressed, allowing the Bridge to apply the command before state is re-evaluated.
_Avoid_: Grace period, settling window, sync window

### Scheduling and Coordination

**Light Registry**:
Owns the runtime collection of Light Control Pairs. Handles Bridge discovery, new light registration (applying the cached command if not excluded), lost light removal, and live LightsToExclude filtering.
_Avoid_: Light store, light manager, light collection

**Light Synchroniser**:
Computes which lights require a Sync and dispatches commands. Groups commands by target state for logging. Fire-and-forget.
_Avoid_: Syncer, light sender, command dispatcher

**Light Controller**:
Top-level facade coordinating the Light Registry and Light Synchroniser. Receives Refresh and Transition commands from the scheduler. Maintains a cached light command for new light initialisation.
_Avoid_: Light manager, controller (alone)

**Schedule Provider**:
Determines whether a transition is due, what type it is, how long it takes, whether it resets Manual Override, and what the target light state should be.
_Avoid_: Transition provider, scheduler (alone)

**Light Scheduler**:
Implements the mode-specific polling loop. Resolves a Schedule Provider, then executes transitions or refreshes based on the provider's decision.
_Avoid_: Scheduler (alone), polling loop

**Light Schedule Worker**:
The background service that drives the polling loop. Calls `scheduler.Execute()` every PollingFrequency seconds and tracks `lastRunTime` and `lastTransitionTime`.
_Avoid_: Worker, background service, poller

**Adaptive Schedule Provider**:
The concrete Schedule Provider for Adaptive mode. Refreshes Solar Events once per day, clamps them to Solar Transition Time Limits, and determines FirstRun, Solar, Adaptive, or Null transition types.
_Avoid_: Adaptive provider, solar provider

**Polling**:
The background loop that runs every PollingFrequency seconds to evaluate whether a transition or sync is due.
_Avoid_: Tick, loop, refresh cycle

### Configuration

**HueShift Options**:
The top-level configuration object. Contains Bridge connection, Geolocation, Colour Temperature bounds, Sleep time, LightsToExclude, transition durations, and polling frequency.
_Avoid_: Config, settings, options (alone)

**LightsToExclude**:
A list of Light IDs or Names in HueShift Options identifying lights the app must never control. Applied live on each poll — no restart required.
_Avoid_: Exclusion list, excluded lights, blacklist

**BasicTransitionDuration**:
The transition duration (seconds) used for Sync commands.
_Avoid_: Sync duration, default transition time

**AdaptiveTransitionDuration**:
The transition duration (seconds) used for Adaptive Transitions.
_Avoid_: Adaptive duration

**SolarTransitionDuration**:
The transition duration (seconds) used for Solar Transitions (Sunrise/Sunset).
_Avoid_: Solar duration, sunrise/sunset duration

**TransitionInterval**:
Seconds between Adaptive Transitions.
_Avoid_: Update interval, polling interval

**PollingFrequency**:
Seconds between polling ticks (each tick decides whether a transition or sync is due).
_Avoid_: Poll rate, tick interval

**NightBrightnessPercentage**:
Brightness percentage applied during the Sleep Window (applied as a fraction of 254).
_Avoid_: Night brightness, sleep brightness, dim level
