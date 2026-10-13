## CatScratch v1.0.9

- Show received party members, classes, GS and CP before combat, including the recorded group-finder party format.
- Clear the temporary party stats at the next loading screen. Repeated packets cannot restore that old preview.
- Keep party previews separate from damage statistics and saved fight history; combat still uses the DPS view.
- Show only rosters containing the freshly identified character on the correct server.
- Reuse unchanged party rows to keep the preview lightweight.

## CatScratch v1.0.8

- Normal startup now requests administrator rights before opening the meter or starting capture.
- Cancelling the Windows permission prompt closes the launch without running the meter.
- Direct updates remain compatible with earlier CatScratch versions; the restarted meter follows the same administrator startup rule.

## CatScratch v1.0.7

- Event type colors: BOSS orange, EVENT teal, PVP purple. Colors remain visible on locked and detached cards.
- Patch notes are available from the meter's document button and Settings > Updates. Installed notes work offline; Refresh from GitHub loads the latest release notes.
- Boss timers keep a solid dark panel behind their content, including in click-through mode.
- Optional per-event sounds: Chime, Bell, Ping or your own WAV file. Preview, volume and reminder timing are configurable in Event sounds.
- Simultaneous sound reminders play in sequence. Each scheduled occurrence is announced once per session. Sounds are disabled until enabled.
- Tracked world boss sounds follow watched bosses and the existing Notify before respawn setting.

## CatScratch v1.0.6

- Renamed the program, executable, shortcuts and GitHub project to CatScratch.
- Direct verified package updates for installed and portable copies, without running Setup again.
- Program file replacement preserves settings, event placement and fight history.
- Failed file replacement restores backups. Updates close the entire meter before restarting.

## v1.0.4

- Reduced repeated event schedule work, temporary allocations and duplicate process searches.
- The interface and refresh rates remain unchanged.

## v1.0.3

- Locked and detached event cards keep their readable backgrounds.
- Pinned card placement survives a complete program restart.
- Corrected event date labels around midnight.
