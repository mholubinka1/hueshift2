# Issues: chore-move-hueshift2-config-logs-pi-media-layout

> Work complete — PR ready to merge.

## Reference compose, README and ADR use the pi-media per-container layout

**GitHub issue**: #434

**Blocked by**: None

**User stories**: 1, 2, 3, 4

### What to build

Point the reference compose's hueshift2 bind mounts at
`/mnt/media/pi-media/containers/hueshift2/{config,log}`, mounted at `/config` and `/log` as
before. Document the layout in the README's Configuration section. Record the decision
as ADR-0004, linking to the home-monitoring ADR-0015 precedent. Gitignore `.claude`.
Application code, the Dockerfile and `appsettings.json` stay untouched.

### Acceptance criteria

- [x] Given the reference `docker-compose.yml`, when its hueshift2 volumes are read, then they are `/mnt/media/pi-media/containers/hueshift2/config:/config` and `/mnt/media/pi-media/containers/hueshift2/log:/log`
- [x] `docker compose config` validates the reference compose
- [x] The hueshift2 service name, `container_name` and host directory are all `hueshift2`
- [x] The README's Configuration section describes the `containers/hueshift2/{config,log}` default and says the paths are host-specific placeholders
- [x] ADR-0004 is present in `.agent-docs/adr/`
- [x] `.claude` is in `.gitignore`
- [x] No change to the Dockerfile, `appsettings.json` or C# sources

---

## Cutover runbook moves the live Pi to the new layout

**GitHub issue**: #435

**Blocked by**: #434

**User stories**: 5

### What to build

A `deployments/CUTOVER_RUNBOOK.md` that moves the running hueshift2 container on the Pi from
`/home/pi/.config/hueshift2` and `/home/pi/.log/hueshift2` to
`/mnt/media/pi-media/containers/hueshift2/{config,log}`. It also covers updating the
`pi-desktop` deploy compose. Walk the operator through it one step at a time after merge.

### Acceptance criteria

- [x] The runbook checks that `/mnt/media` is mounted before creating anything under it
- [x] The runbook copies the config and logs with plain `cp`, because the media drive does not keep ownership or permissions, and keeps the old directories until verification passes
- [x] The runbook includes rollback steps
- [x] Given the live hueshift2 on `/home/pi` paths, when the runbook is followed, then `docker inspect hueshift2` shows the new mounts
- [x] The app loads the existing config after cutover: the bridge key is intact and no fresh default is written
- [x] New log lines appear under `/mnt/media/pi-media/containers/hueshift2/log/`
- [x] The `pi-desktop` `docker/docker-compose.yml` hueshift2 volumes match the reference compose

---
