# CatScratch

A separate always-on-top Windows damage meter for AION 2 by Zerafithekitten.

[Download the latest release](https://github.com/Zerafithekitten/CatScratch/releases/latest) · [Patch notes](data/patchnotes-en.md) · [Report an issue](https://github.com/Zerafithekitten/CatScratch/issues)

Latest published version: **2.4.5**. English is the default; German, Russian and French are available in Settings.

## Preview

CatScratch UI rendered with sample data. Names and numbers are examples. The screenshots show the current interface.

### DPS meter and Personal combat

<img src="docs/screenshots/meter.png" alt="CatScratch 2.4 DPS meter with party damage taken and personal healing" width="420" />

### Click-through mode

The outer window background, borders and controls disappear; subtle dark player backgrounds keep names and numbers readable. DPS bars, personal values, clock and ping remain visible. The green backdrop below represents the game behind the transparent window.

<img src="docs/screenshots/meter-click-through.png" alt="Transparent click-through DPS meter and personal statistics" width="420" />

### Narrow window

The meter can be resized down to 260 px wide. Long names and CP/GS labels shorten when space is limited; the footer adapts to keep its controls accessible.

<img src="docs/screenshots/meter-narrow.png" alt="CatScratch at its 260 px minimum width" width="260" />

### Detailed fight statistics

Dark player cards, a separate summary row, teal tabs, clearer skill tables and a class-colored timeline replace the older gold layout. Player selection, sorting, rotation, defense and chat/image export remain available.

<img src="docs/screenshots/fight-details-next.png" alt="CatScratch fight details with player cards and DPS timeline" width="1040" />

<details><summary>Accuracy and German interface previews</summary>

<img src="docs/screenshots/fight-accuracy-next.png" alt="Accuracy dashboard with separate hit-quality cards" width="1040" />

<img src="docs/screenshots/fight-details-de-next.png" alt="German fight details at the minimum supported window width" width="900" />

</details>

### Compact fight selection and Patch notes

Available since 2.1.2. Saving fights is optional: press the save button after combat. The picker switches between the live fight, current-session fights and the last ten saved fights.

<img src="docs/screenshots/fight-picker.png" alt="Compact CatScratch fight selection" width="340" />

<img src="docs/screenshots/patch-notes.png" alt="CatScratch Patch notes with a dark custom header" width="580" />

### Settings and upcoming events

<img src="docs/screenshots/settings.png" alt="Organized CatScratch settings" width="760" />

<img src="docs/screenshots/event-settings.png" alt="Event preferences" width="470" />

<img src="docs/screenshots/upcoming-events.png" alt="All selected events within the next hour" width="520" />

<details><summary>Boss timers and optional event sounds</summary>

<img src="docs/screenshots/boss-timers.png" alt="CatScratch boss timers" width="760" />

<img src="docs/screenshots/event-sounds.png" alt="Optional per-event sounds with the ten-second custom WAV limit" width="580" />

</details>

### Pet tracker and shortcuts

Use the paw button beside the CatScratch title to toggle the compact Pet tracker. Only personally attacked, confirmed pet-source monsters add a row: a paw symbol, pet name, soul count such as 11/25 and a thin progress bar toward the next level. Soul loot updates the count after the kill. The three most recently attacked pets are retained, newest first; maxed pets show MAX; repeated hits do not add duplicate rows. Click-through hides the header and outer frame, leaving the compact rows visible. Optional unlock/level-up sounds remain behind the settings button. Own progress is saved per character across restarts. A ~ prefix marks last-known counts until fresh login data arrives. If no saved baseline exists, log into the character once while CatScratch is running; opening an already cached pet menu does not necessarily resend the collection. Recent completed local recordings can recover a missing baseline. The catalog includes confirmed Global monster-source mappings for 165 pets; unknown mappings remain explicit. No nearby scan, location, distance, map or radar is used.

<img src="docs/screenshots/pet-tracker.png" alt="Compact attacked-pet tracker with name, soul count and progress" width="300" />

All shortcuts can be recorded by pressing a combination in **Settings > Hotkeys**. Defaults:

| Action | Shortcut |
| --- | --- |
| DPS meter | Shift+F5 |
| Personal combat | Shift+F6 |
| Upcoming events | Shift+F7 |
| Boss timers | Shift+F8 |
| Pet tracker | Shift+F9 |
| All windows | Shift+F10 |
| Click-through | Shift+F11 |
| Restart meter | Shift+F12 |

The all-window shortcut restores the same previously open windows and their positions. The installed version is always visible at the bottom left of the meter.

<img src="docs/screenshots/pet-tracker-click-through.png" alt="Pet tracker in click-through mode with only its compact row visible" width="300" />

### Update improvements

- Subtle dark backgrounds behind Personal combat rows in click-through mode for readability.
- Remove the update's old staging copy, rollback backup and downloaded ZIP after the updated app starts successfully. Failed-update recovery files are retained.

Available in version 2.2.0.

## Languages

Select the language in **Settings > General > Language**. English remains the default; patch notes remain English.

| Language | Interface | Availability |
| --- | --- | --- |
| English | English | Current release (2.4.5) · default |
| German | Deutsch | Current release (2.4.5) |
| Russian | Русский | Current release (2.4.5) |
| French | Français | Current release (2.4.5) |

Skill and monster names use the bundled language tables, with English as the fallback for missing names. Player names are preserved.

## Bugs and feature requests

[Report a bug or request a feature](https://github.com/Zerafithekitten/CatScratch/issues/new/choose). Choose the appropriate form; bug reports and feature requests must be written in English only. A GitHub account is required.

Use the **Report a bug / request a feature** button in **Settings > General** which opens this page in your browser. No separate server is needed. CatScratch does not submit anything or attach logs or packet captures automatically.

Resource usage: a shared limit of four simultaneous optional image downloads, five-minute retry backoff after temporary failures, cancellation on exit and coalesced statistics redraws when images arrive. The meter's layout stays the same.

## Downloads

Get Setup or the portable ZIP from [Releases](https://github.com/Zerafithekitten/CatScratch/releases/latest).
Install once with Setup, or unzip the portable package. Both can update directly in the app.
Install [Npcap](https://npcap.com/#download) separately, start the meter and teleport once.
Normal startup requests administrator rights through Windows. If you cancel that prompt, the meter does not start.
Use borderless windowed mode to keep the meter visible above the game.

## Features

- Own damage while solo and confirmed current party members while grouped.
- Class, name, DPS, damage, share, Combat Power and Gear Score.
- Dark player rows with separate class-colored DPS bars, resizing, opacity and shortcuts recorded by pressing keys.
- Damage taken in red on every player row; integrated, collapsible Personal combat for self healing and healing of others.
- Click-through removes window frames while preserving damage, time and RTT.
- Movable cards for all selected events and enabled world-boss timers within the next hour, with a pulsing border for two minutes after the start.
- Separate observed boss timers; optional fight saving, retaining the last ten saved fights.
- Colored EVENT / PVP / BOSS badges and readable boss panels when locked.
- Optional per-event sounds with preview, volume, reminder timing and custom PCM/float WAV files up to ten seconds.
- Patch notes available in the meter, including offline notes for the installed version.
- English by default, selectable German, Russian and French in Settings > General > Language; patch notes remain English.
- Verified GitHub updates for installed and portable copies.

Use the meter's **Check for updates** button, or Settings > General > Updates > Check now. Checking only checks; installation starts when you press **Update now**.
CatScratch replaces its program files and restarts without running Setup. Its own userdata is preserved; failed file replacement restores backups. Updates need a writable program folder.
Update downloads and temporary staging are kept beside the running executable under `userdata/temp/updates`, including portable installations. Settings, captured packets and saved fights are stored separately and are not update cleanup targets.
Existing Zerafi DPS Monitor installations use one final migration installer to switch the executable and shortcuts to CatScratch. The installation identity and userdata stay the same; the old GitHub URL redirects to this repository.
The main window's X exits the whole program, including event and boss windows.
The show/hide shortcut only changes visibility.

The meter passively captures traffic. No game-memory access, injection, game-file
changes, outgoing game packets or simulated game input are implemented.
This does not imply publisher approval. Incoming packets can contain other players;
the results are filtered to your confirmed character and party.

## Limitations

Missing server data can make damage incomplete. RTT is a passive TCP estimate;
a local accelerator can expose only its local connection. Events use bundled
community schedules from [Shugo.GG](https://shugo.gg/timers), checked Oct 12, 2026.
Global event times are not officially confirmed. Verify availability in game.

## Development

Requires .NET 10. See [GITHUB-RELEASES.md](GITHUB-RELEASES.md).
Version tags build the Windows app and installer and publish release downloads.
Packet recordings and userdata are excluded from Git and releases.

## License

GPL-3.0, based on [cyberbadger6969/aion2-dps-meter](https://github.com/cyberbadger6969/aion2-dps-meter),
base revision 414de5c1d5f3d45ce35701bd25229c2dba736bb6.
Keep LICENSE and data/NOTICE.md with redistributions. Full source accompanies releases.
AION 2 belongs to NCSOFT.

### Recent debug buffer (experimental)

In Settings → Capture & data, enable Recent debug buffer and save. Save recent debug exports up to the previous 60 seconds to userdata/captures beside the running application. The buffer is disabled by default and capped at 32 MB of accounted packet storage; high traffic can shorten the interval. Continuous recording remains available. An export can temporarily retain its snapshot while capture continues. Only packets received while the buffer is enabled are available; a short recording may lack earlier protocol or character context.
