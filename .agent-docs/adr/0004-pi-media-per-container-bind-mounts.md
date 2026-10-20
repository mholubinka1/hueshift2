# Host bind mounts live under `pi-media/containers/hueshift2/`

hueshift2's config and logs used to live in the Pi user's home directory on the SD card:
`/home/pi/.config/hueshift2` and `/home/pi/.log/hueshift2` in the deployed compose. The
reference `docker-compose.yml` had a different log path again (`/home/pi/hueshift2`).
The other single-container stacks on the host (`bin-reminder`,
`hypervolt-agile-scheduler`, `octopus-app`, `hive-app`, `home-monitoring-db`) keep their
state under one per-container root on the media drive, named after the
`container_name`. bin-reminder's compose service is still `bromley-bin-reminder`. home-monitoring
[ADR-0015](https://github.com/mholubinka1/home-monitoring/blob/main/.agent-docs/adr/0015-pi-media-per-container-bind-mounts.md) and its
2026-10-02 amendment state the rule: **a container's compose service name, its
`container_name` and its directory under `/mnt/media/pi-media/containers/` are the same
string.**

hueshift2 now follows that rule:

```text
/mnt/media/pi-media/containers/hueshift2/config  -> /config
/mnt/media/pi-media/containers/hueshift2/log     -> /log
```

The mount points inside the container don't change. The entrypoint still reads
`/config/hueshift2-config.json`, and Serilog still writes to `/log`, so no application code,
`appsettings.json` or Dockerfile change was needed. The host log leaf is `log`, matching
the sibling stacks.

## Considered Options

- **Keep the home-directory paths.** Rejected. They split this container's state across
  two dot-directories on the SD card and leave hueshift2 as the only stack off the
  per-container convention.
- **Move only the logs** (the part that grows). Rejected. It leaves no single path for
  "everything hueshift2 writes", and that path is the point of the convention.

## Consequences

- The host paths in `docker-compose.yml` are placeholders that a personal deployment is
  expected to edit. This ADR records the default layout. It is not a requirement.
- The live deploy compose lives in the `pi-desktop` repo, and it diverges from this
  repo's reference until it is updated. The cutover is in
  `deployments/CUTOVER_RUNBOOK.md`.
- The config file holds the bridge API key, so the cutover must copy it. If the app
  starts without `/config/hueshift2-config.json`, it generates a fresh config with no
  API key, and the bridge has to be paired again.
