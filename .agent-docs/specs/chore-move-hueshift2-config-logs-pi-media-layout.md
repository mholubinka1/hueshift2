# Move hueshift2 config and logs to the pi-media per-container layout

## Problem Statement

On the Pi, every other single-container stack (`bin-reminder`, `hypervolt-agile-scheduler`,
`octopus-app`, `hive-app`, `home-monitoring-db`) keeps its state under one root on the media
drive: `/mnt/media/pi-media/containers/<name>/`. hueshift2 is the odd one out. Its config
lives in `/home/pi/.config/hueshift2` and its logs in `/home/pi/.log/hueshift2`, both on the
SD card. The reference `docker-compose.yml` in this repo shows yet another log path,
`/home/pi/hueshift2`. So there is no single place that holds everything hueshift2 writes,
and the reference compose doesn't match what's deployed.

## Solution

hueshift2 follows the host's per-container layout. Its compose service name, its
`container_name` and its directory under `/mnt/media/pi-media/containers/` are the same
string, `hueshift2`:

```text
/mnt/media/pi-media/containers/hueshift2/config   -> /config
/mnt/media/pi-media/containers/hueshift2/log      -> /log
```

The paths inside the container don't change. The entrypoint still reads
`/config/hueshift2-config.json`, and Serilog still writes `/log/hueshift2-.log` and
`/log/hueshift2-debug-.log`, so the application doesn't change at all. The reference
compose, README and a new ADR record the layout. A cutover runbook moves the live Pi
across.

## User Stories

1. As the Pi operator, I want hueshift2's config and logs under
   `/mnt/media/pi-media/containers/hueshift2/`, so that all of this container's state sits
   in one place on the media drive, next to every other stack's.
2. As the Pi operator, I want the reference `docker-compose.yml` to show the same host
   paths as the deployed compose, so that copying it gives the right layout.
3. As a first-time deployer, I want the README to explain the `config/` and `log/`
   directories and what goes in each, so that I can set up the host before the first
   `docker compose up`.
4. As a maintainer, I want an ADR that records why this layout was chosen and links to
   the cross-repo precedent, so that a later change doesn't drift back to home-directory
   paths.
5. As the Pi operator, I want a step-by-step cutover runbook, so that the live container
   moves to the new paths without losing its config (bridge API key, geolocation) or its
   log history.

## Implementation Decisions

- **Layout rule:** compose service name == `container_name` == directory name under
  `/mnt/media/pi-media/containers/`. For hueshift2 that string is `hueshift2`. This is the
  same rule as home-monitoring ADR-0015 (2026-10-02 amendment).
- **Host log leaf is `log`**, matching the sibling stacks. The container mount is already
  `/log`, so the two names agree.
- **Container mount points are unchanged** (`/config`, `/log`). The Dockerfile,
  `appsettings.json` and the C# code are not touched.
- **Reference compose** (`docker-compose.yml`) volumes change to the new host paths. Its
  other settings (ports, TZ, watchtower label, restart policy) are not touched.
- **README:** the Configuration section names the default host layout and says the paths
  are host-specific placeholders.
- **ADR-0004** records the decision, the options that were rejected, and the cutover
  consequence.
- **Cutover runbook:** a new `deployments/CUTOVER_RUNBOOK.md`. It is stepped through with
  the user one step at a time after the dev work merges.
- **Housekeeping:** `.claude` is added to `.gitignore` so that agent worktrees are never
  committed.

## Testing Decisions

- No automated test. The change is deployment configuration with no code path. We
  considered a compose-layout test (home-monitoring has `test_deployment_layout.py`) and
  chose not to add one for this change.
- Verification is behavioural, at the deployment seam, during the cutover:
  `docker compose config` resolves the new volumes. After the container is recreated,
  `docker inspect hueshift2` shows the new mounts, the app loads the existing config
  without generating a fresh default, and new log lines appear under
  `/mnt/media/pi-media/containers/hueshift2/log/`.

## Out of Scope

- Editing the `pi-desktop` repo's `docker/docker-compose.yml` as part of this PR. That
  repo is updated during the cutover runbook.
- Changing paths inside the container, log file names, or Serilog configuration.
- Migrating any other stack.
- Adding an automated layout test.

## Further Notes

- Precedent: home-monitoring `.agent-docs/adr/0015-pi-media-per-container-bind-mounts.md`
  and its `deployments/CUTOVER_RUNBOOK.md`.
- Live deploy source of truth: `/home/pi/git/pi-desktop/docker/docker-compose.yml`
  (project name `docker`). Current hueshift2 mounts there are
  `/home/pi/.config/hueshift2:/config:rw` and `/home/pi/.log/hueshift2:/log:rw`.
- The existing config file holds the bridge API key, and it must be copied, not
  regenerated. If `/config/hueshift2-config.json` is missing at startup, the app generates
  a fresh config with no API key, and the bridge has to be paired again.
