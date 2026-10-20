# Pi cutover runbook: hueshift2 → `pi-media/containers/hueshift2/`

This runbook moves the live hueshift2 container's config and logs off the SD card and into the per-container layout on the media drive ([ADR-0004](../.agent-docs/adr/0004-pi-media-per-container-bind-mounts.md)):

| | From | To |
|---|---|---|
| `/config` | `/home/pi/.config/hueshift2` | `/mnt/media/pi-media/containers/hueshift2/config` |
| `/log` | `/home/pi/.log/hueshift2` | `/mnt/media/pi-media/containers/hueshift2/log` |

Run it in one supervised session on the Pi. hueshift2 is down for under a minute, so lights hold their current state and are not adjusted during that time. Nothing changes inside the container, so there is no image change or app config edit.

Names used below, on the Pi (`pi-desktop`):

```bash
COMPOSE=/home/pi/git/pi-desktop/docker/docker-compose.yml
OLD_CONFIG=/home/pi/.config/hueshift2
OLD_LOG=/home/pi/.log/hueshift2
NEW=/mnt/media/pi-media/containers/hueshift2
```

The live compose file is in the separate `pi-desktop` repo and may hold uncommitted edits. Every edit below is a minimal, anchored change made after a backup. Never replace the file wholesale.

The media drive (NTFS) does not keep timestamps or permissions, so use plain `cp` when copying onto it. If you use `cp -p`, it prints "Operation not permitted" warnings there, but the copy still happens. The compose file is on the SD card, so editing it raises no such warnings. The image sets no `USER`, so the container runs as root and can write `log/` without a `chown`.

## Pre-checks

1. Confirm the media drive is mounted. fstab mounts it at `/mnt/media/pi-media`. If it isn't mounted, the container would write into an empty directory on the SD card:

   ```bash
   mountpoint /mnt/media/pi-media
   ```

   It must print `/mnt/media/pi-media is a mountpoint`.

2. Confirm the current mounts and that the config file exists:

   ```bash
   docker inspect hueshift2 --format '{{range .Mounts}}{{.Source}} -> {{.Destination}}{{"\n"}}{{end}}'
   ls -la "$OLD_CONFIG" "$OLD_LOG"
   ```

   Expected: `$OLD_CONFIG -> /config` and `$OLD_LOG -> /log`, and `hueshift2-config.json` in `$OLD_CONFIG`.

3. Confirm the destination doesn't exist yet. A rerun must not overwrite a config that is already in place:

   ```bash
   [ ! -e "$NEW" ] && echo "preconditions OK" || echo "STOP: $NEW already exists; inspect it before going on"
   ```

4. Check who will be able to read the bridge API key. The config moves onto the NTFS drive, where the fstab mask sets a single mode for every file, and Samba exports the drive ([ADR-0004](../.agent-docs/adr/0004-pi-media-per-container-bind-mounts.md)):

   ```bash
   grep pi-media /etc/fstab
   testparm -s | grep -B3 -A12 'path *= */mnt/media'
   ```

   Look at the `fmask`/`umask` in the fstab line and the users the share allows. If the `testparm` line prints nothing, empty output is not a pass: read the full `testparm -s` output for any share that covers `/mnt/media/pi-media`. Decide that it is acceptable for them to read the key before continuing.

Do not continue to the Procedure unless pre-check 1 printed `is a mountpoint`, pre-check 3 printed `preconditions OK`, and pre-check 4 is accepted.

## Procedure

1. Stop the container, so nothing writes while the files are copied:

   ```bash
   docker compose -f "$COMPOSE" stop hueshift2
   ```

   From here on, any STOP in steps 2–4 leaves hueshift2 down. To bring it back on the old paths, run [Rollback](#rollback). It is safe even if the compose file was never edited. Then run `rm -r "$NEW"` so that pre-check 3 passes on a retry.

2. Create the directories and copy the config and logs. The old directories stay in place as the rollback:

   ```bash
   mkdir -p "$NEW/config" "$NEW/log"
   cp -r "$OLD_CONFIG/." "$NEW/config/"
   cp -r "$OLD_LOG/." "$NEW/log/"
   cmp "$OLD_CONFIG/hueshift2-config.json" "$NEW/config/hueshift2-config.json" && echo "config copied intact"
   diff -rq "$OLD_LOG" "$NEW/log" && echo "logs copied intact"
   ls -la "$NEW/config" "$NEW/log"
   ```

   Stop if `config copied intact` or `logs copied intact` does not print.

3. Edit the live compose file. Back it up, swap the two hueshift2 volume lines, then review the diff and validate:

   ```bash
   BAK="$COMPOSE.bak-$(date +%Y%m%d%H%M%S)"
   cp "$COMPOSE" "$BAK"
   sed -i \
     -e 's#- /home/pi/.config/hueshift2:/config:rw#- /mnt/media/pi-media/containers/hueshift2/config:/config#' \
     -e 's#- /home/pi/.log/hueshift2:/log:rw#- /mnt/media/pi-media/containers/hueshift2/log:/log#' \
     "$COMPOSE"
   diff "$BAK" "$COMPOSE"
   docker compose -f "$COMPOSE" config --format json | python3 -c "import json,sys; print([(v['source'], v['target']) for v in json.load(sys.stdin)['services']['hueshift2']['volumes']])"
   ```

   The diff must show only the two hueshift2 volume lines. If it is empty, `sed` matched nothing: STOP and compare the live hueshift2 volume lines with the patterns above. The last command must print the two new mounts.

4. Recreate the container on the new mounts:

   ```bash
   docker compose -f "$COMPOSE" up -d hueshift2
   ```

## Verify

1. The container is running with the new mounts:

   ```bash
   docker ps --filter name=hueshift2 --format '{{.Names}}\t{{.Status}}'
   docker inspect hueshift2 --format '{{range .Mounts}}{{.Source}} -> {{.Destination}}{{"\n"}}{{end}}'
   ```

2. The existing config was loaded and not regenerated. At startup the app logs `Asserting ... Hue bridge settings` for an existing file and `Generating ...` for a missing one:

   ```bash
   docker logs hueshift2 2>&1 | grep -E 'Asserting|Generating|does not exist|Bridge reachable|IP mismatch'
   cmp "$OLD_CONFIG/hueshift2-config.json" "$NEW/config/hueshift2-config.json" && echo "config unchanged"
   ```

   Pass: `Asserting /config/hueshift2-config.json ...` with no `Generating` or `does not exist`, then `config unchanged`. You will usually also see `Bridge reachable at ...`, but if the bridge didn't answer at that moment the app runs discovery instead, and that is not a failure. (`docker logs` covers only the recreated container, so these are this start's lines.) A `cmp` difference is expected only if the logs also show `IP mismatch` (the app rewrote the bridge IP) or a fresh bridge pairing. In any other case, treat it as a failure and go to Rollback.

3. New log lines are appearing on the media drive. Wait a minute after Procedure step 4, then:

   ```bash
   ls -lt "$NEW/log" | head -3
   tail -5 "$(ls -t "$NEW"/log/hueshift2-*.log | grep -v debug | head -1)"
   docker logs --since 5m hueshift2 2>&1 | tail -10
   ```

   The newest log file has entries timestamped after Procedure step 4, and they show no config or bridge errors.

4. Commit the compose change in the `pi-desktop` repo. If the file has other uncommitted edits, commit only the hueshift2 hunk. Then record the commit for Rollback:

   ```bash
   SHA=$(git -C /home/pi/git/pi-desktop rev-parse HEAD); echo "$SHA"
   ```

## Clean up

Do this only after Verify passes, ideally after a day of normal running. This is probably a new shell, so declare the names again (see the top of this runbook) and check which backup will be removed:

```bash
COMPOSE=/home/pi/git/pi-desktop/docker/docker-compose.yml
OLD_CONFIG=/home/pi/.config/hueshift2
OLD_LOG=/home/pi/.log/hueshift2
BAK=$(ls "$COMPOSE".bak-* | tail -1); echo "$BAK"
```

Check that `$BAK` is the backup from this cutover (the newest one), then remove the old directories and that backup:

```bash
rm -r "$OLD_CONFIG" "$OLD_LOG"
rm "$BAK"
```

## Rollback

Use this if a Procedure step stops or any Verify step fails before Clean up. In a new shell, declare the names from the top of this runbook first. Rollback reverses only the two hueshift2 volume lines, so any other edits made to the live compose since the backup are kept.

1. Stop the container:

   ```bash
   docker compose -f "$COMPOSE" stop hueshift2
   ```

2. Put the old volume lines back. Use **one** of these:

   - If Verify step 4 already committed the compose change in `pi-desktop`, revert that commit. Then the repo and the live file agree, and a later pull won't bring the new mounts back:

     ```bash
     git -C /home/pi/git/pi-desktop revert "$SHA"   # the value printed in Verify step 4
     ```

   - Otherwise, reverse the two lines in place:

     ```bash
     sed -i \
       -e 's#- /mnt/media/pi-media/containers/hueshift2/config:/config#- /home/pi/.config/hueshift2:/config:rw#' \
       -e 's#- /mnt/media/pi-media/containers/hueshift2/log:/log#- /home/pi/.log/hueshift2:/log:rw#' \
       "$COMPOSE"
     ```

3. Recreate the container and confirm it is back on the old mounts:

   ```bash
   docker compose -f "$COMPOSE" up -d hueshift2
   docker inspect hueshift2 --format '{{range .Mounts}}{{.Source}} -> {{.Destination}}{{"\n"}}{{end}}'
   ```

The old directories were never changed, so hueshift2 comes back with its pre-cutover config and logs. Anything written after the cutover stays only in `$NEW`: log lines, and any bridge-IP rewrite in the config. Copy it back if you need it. Leave `$NEW` in place to investigate, or remove it before a retry so pre-check 3 passes.
