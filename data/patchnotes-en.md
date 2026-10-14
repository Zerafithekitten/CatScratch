## CatScratch v2.5.0-beta.1

- Add selectable Stable and Beta update channels in Settings > General. Stable remains v2.4.8; beta builds are prereleases and never replace the stable download. Checking or switching channels never installs a release. Updates require Update now; returning to Stable preserves settings and saved fights.
- Add a start/stop Test mode in Settings > General and the tray menu. Run sample fights with HP, damage, incoming damage and healing, plus sample pet progress, selected events and a separate boss timer. Test data stays in isolated trackers and cannot be saved as real fights; stopping restores the real character, party and collection state.
- Add global UI scaling from 75% to 150% in Settings > Appearance. Scale text and icons independently of row size and preserve the saved window width.
- Keep modules fully inside one monitor while dragging, resizing and restoring positions. Allow flush placement at screen edges; account for per-monitor DPI and negative monitor coordinates. Interactive dialogs stay within the desktop work area.
- Add Stream mode in Settings > Appearance. Use stable Cat 1, Cat 2, Cat 3 aliases in rows, tooltips, Personal combat, detailed statistics, chart labels and copied reports. Local packet recordings and saved fight files retain their original data.
- Improve automatic fight ending: end non-boss fights when all attacked targets are confirmed dead; keep ongoing fights active while confirmed party members heal, receive damage or keep hitting, or boss HP changes. Repeated unchanged HP and foreign healers do not extend fights. Finished results stay available until the next fight. The inactivity setting remains configurable; confirmed deaths and resets still close the fight.
- Keep English as the default and add German, Russian and French translations for the new controls. Retain click-through backgrounds and passive-only game access.

## CatScratch v2.4.8

- Respect Show boss HP bar in compact mode. Remove the compact-layout override that hid the bar even when enabled.
- Keep the existing slim HP bar visible in normal and click-through views when boss HP data is available. Turning the setting off still hides it; no missing HP values are invented.
- Add regression checks for compact and normal views, click-through and the disabled setting. Refresh the meter previews on GitHub.

## CatScratch v2.4.7

- Handle custom-audio validation failures inside the event sound window instead of allowing InvalidDataException to escape the asynchronous selection callback.
- Show red inline error messages under the affected event. Explain excessive duration with the measured seconds, missing or empty files, the 10 MB limit, unsupported formats and Windows decoder failures.
- Keep the previous sound and filename after a rejected or cancelled selection. Preserve the 10-second limit.
- Add end-to-end asynchronous sound selection, preview, rejection and cancellation checks to the release build. Measure PCM/float WAV duration directly from sample data; retain Windows decoder support for MP3 and other WAV encodings.

## CatScratch v2.4.5

- Open the file picker immediately when selecting custom audio without an existing file. Cancelling or rejecting the file preserves the previous sound selection.
- Let Preview request a replacement when a saved custom file is missing, then validate and preview the selected WAV or MP3.
- Display the custom audio filename in the sound dropdown, with its full path in the tooltip. Rename the file button to Change audio after selection.
- Preserve legacy custom WAV preferences and the maximum of 10 seconds / 10 MB.

## CatScratch v2.4.4

- Accept custom WAV and MP3 event sounds through the Windows audio decoder, including WAV encodings rejected by the previous PCM-only check.
- Validate decoded duration when choosing or previewing custom audio. Keep the maximum at 10 seconds and 10 MB; reject invalid files and unknown durations.
- Recheck queued files and wait for the decoder duration check before playback starts.
- Preserve existing Custom WAV preferences and file paths. Update the file picker, labels and translations for WAV and MP3 support.

## CatScratch v2.4.3

- Add an optional experimental recent-debug buffer alongside continuous packet recording. Enable it in Settings > Capture & data and save the settings.
- Retain up to 60 seconds of passively received packets with a 32 MB accounting limit. High traffic can shorten the retained interval; disabling the buffer clears its history.
- Save recent debug exports asynchronously as PCAP files under userdata/captures beside the running application. No file is created until export; exports are serialized to avoid overlapping saves.
- Keep the buffer disabled by default and translate its controls into German, Russian and French. This remains passive: no game data is modified and no packets are sent.

## CatScratch v2.4.2

- Rebind a respawned player’s new actor ID to the same named/server-scoped participant in an active fight, preserving damage, healing, incoming damage and detailed skill statistics. Unknown actors and same-name foreign-server players cannot take over a party row.
- Tighten pet-row padding and gaps. Show MAX and a full progress bar for maxed pets. Limit the compact tracker to the three most recently attacked pets, newest first.
- Preserve last-known pet progress per character across restarts, with debounced atomic writes. Recover a missing baseline from recent completed own recordings; never overwrite a fresh server snapshot. Cached non-max counts carry a ~ marker and tooltip until current login data arrives.
- Replace the old collection/change-zone warning with Attack a monster to see pet progress. If an attacked pet is known but its own collection baseline is unavailable, say that progress data has not arrived rather than inventing a count.

## CatScratch v2.4.1

- Replace proximity-based pet discovery with compact progress for the own player’s attacked monster. Show only a paw symbol, pet name, confirmed soul count (such as 11/25) and a thin progress bar for the next level (5 to unlock, then 25/75). Track only personally attacked source species, bounded to eight recent pets. Retain progress after the kill so soul loot can update it; no location, distance, map or nearby scan is used.
- Rename the meter shortcut to Show / hide DPS meter.

## CatScratch v2.4.0

- Add a compact, separate Pet tracker with a paw button, collection/level goals, genus/faction/search filters and optional unlock/level-up sounds. Read confirmed own pet levels and soul increments passively; preserve progress across loading screens. Unlock pets with 5 souls, then require 25/75 more for levels 2/3.
- Include confirmed Global monster-source mappings for 165 pets, with verified locations where available, observed source monsters and manual farming-reference links. Unknown sources/locations remain explicit; no map or radar is included.
- Show damage taken in red beneath every player's contribution percentage. Keep Personal combat focused on self healing and healing of others.
- Keep the installed version visible at the bottom left, including click-through mode.
- Add separately configurable visibility shortcuts for each module and all windows together. Record combinations by pressing them in Settings. Default shortcuts are Shift+F5 through Shift+F12, away from letter keys; migrate old defaults once while retaining custom bindings.
- Preserve window positions and open modules when hiding all windows, suppress automatic visibility while hidden, and keep event toggling independent of click-through mode.

## CatScratch v2.3.2

- Stop opening extra event windows on double-click. Separate cards are available only through the explicit right-click action and now match the compact single-line layout.
- Update the native layout checks and validate diagnostic output during release builds.

## CatScratch v2.3.1

- Display Upcoming events in compact single-line cards: event type and name on the left, countdown on the right. Remove scheduled clock times while retaining the two-minute started-event pulse.

## CatScratch v2.3.0

- Make Upcoming events cards more compact without shrinking the text. Show today's start times without the Today label; retain Tomorrow/date labels across midnight.

- Add selectable Russian and French interfaces alongside English and German. Translate the settings, meter, fight statistics, event panels, sounds and tray menu; load the bundled language-specific game names. English remains the default; patch notes remain English.
- Add a feedback button in Settings > General that opens GitHub's bug/feature form chooser. A GitHub account is required; nothing is submitted automatically and no logs or captures are attached.
- Limit optional icon/portrait downloads to four concurrent requests, back off for five minutes after temporary failures and cancel pending requests on exit. Coalesce concurrent image-ready redraws and skip reloading closed statistics windows.
- Correct the event-settings hint to describe every selected event within the next hour, and keep recent-event labels localized.

## CatScratch v2.2.0

- Show all selected events and enabled world-boss timers within the next hour, ordered by start time. Keep recently started events visible for two minutes with pulsing borders while later cards remain visible.
- Redesign detailed fight statistics with dark player cards, separate summary tiles, teal tabs and clearer DPS/accuracy tables. Preserve player selection, sorting, rotation, defense and chat/image export.
- Keep a subtle dark background behind Personal combat rows in click-through mode; the surrounding window remains transparent.
- Clean up the direct update's staging copy, backup and downloaded package after successful application startup. Wait for the staging helper to exit, preserve failed-update recovery files and validate cleanup paths.

## CatScratch v2.1.2

- Make the fight picker narrower with shorter rows, smaller portraits and CatScratch's dark surfaces and teal accents.
- Correct the empty fight hint: saving fight statistics is optional and available through the save button.
- Restyle Patch notes with a custom paw header, compact controls and readable dark cards. Remove the native light title bar; preserve scrolling, GitHub refresh and embedded previews.

## CatScratch v2.1.1

- Keep event sound preferences exclusively in Event settings; remove their shortcut from the meter settings.
- Restyle Boss timers and Event sounds with CatScratch's dark cards, teal controls and paw headers.
- Remove the native white strip from Event settings and Event sounds windows.
- Add selectable German interface, class labels, menus, reminders, skill and monster names. English remains the default; patch notes remain English.
- Reject custom WAV files longer than 10 seconds. Validate actual PCM/float sample duration when selecting, previewing and playing sounds, including previously configured files.
- Retain completed dungeon fight results until the next fight, so they remain readable and can be saved. Group membership survives loading without switching back to CP/GS preview inside the dungeon.
- Log CatScratch window visibility changes to help diagnose intermittent popups. This adds diagnostics; the reported popup cause is not yet confirmed.

## CatScratch v2.1.0

- Remove the unused View button from the DPS footer; display options remain available in Settings.
- Reorganize Settings into Appearance, Combat, Hotkeys, History, Capture & data, and General. Group window, player display and visibility options separately.
- Match both Settings and Event settings to the meter's dark rounded surfaces, teal accents and readable controls.
- Make Personal combat more compact with shorter rows and tighter spacing.
- Keep selected events visible for two minutes after their scheduled start. Show Started and pulse only the colored border, including detached automatic cards.
- Keep the nearest-start and one-hour event filters. Stop animations when events expire or windows close.

## CatScratch v2.0.3

- Keep a subtle dark background behind each player in click-through mode for better readability; the outer window remains transparent.
- Allow window widths down to 260 px, including in Settings. Adapt the title bar, value columns and footer at narrow widths.
- Display embedded GitHub preview images inside Patch notes. Load previews only when viewing notes, in memory, with download limits and an offline fallback.

## CatScratch v2.0.2

- Make individual player rows noticeably more compact: shorter cards, tighter gaps, smaller class emblems and thinner DPS bars.
- Keep names, CP/GS, DPS, total damage and percentages readable in the same two-line design.
- Preserve Personal combat and transparent click-through behavior.

## CatScratch v2.0.1

- Make DPS rows slightly more compact by reducing row height, vertical padding and spacing.
- Preserve fonts, colors, the overall design and transparent click-through behavior.

## CatScratch v2.0.0

- Redesign the DPS meter with dark rounded player rows, larger class emblems and separate class-colored damage bars.
- Integrate Personal combat below the DPS list with subtle gold, mint and blue accents. No separate personal window is opened.
- Collapse or expand personal totals; keep damage taken and healing independently optional.
- Show personal totals for the same live, selected or saved fight as the DPS list.
- In click-through mode hide window backgrounds, row surfaces, borders and controls while keeping DPS bars, personal values, clock and ping visible.
- Update window presets and initial sizing for the new layout. Keep existing preferences and update behavior.

## CatScratch v1.0.27

- Match Personal combat to the DPS meter with its purple paw, teal title, font and compact icon buttons.
- Align the close button to the right at every window width.
- Replace bright gray waiting rows and unknown-class icons with subdued dark placeholders.
- Keep class-colored metric rows visible when locked.

## CatScratch v1.0.26

- Restyle Personal combat with compact class-colored rows matching the DPS meter.
- Hide its window background, frame and title bar when click-through is enabled; keep metric rows visible.
- Add a personal-module toggle button beside the CatScratch title.

## CatScratch v1.0.25

- Remove the unused Events button from the DPS meter.
- Add Show all world bosses to Upcoming events preferences. Include scheduled bosses and known world-boss respawns received from the game, keeping the next-start and one-hour filters.
- Separate Show gear score (GS) and Show combat power (CP), including group previews. Preserve the previous combined preference until changed.
- Add an optional Personal combat window showing your damage taken and healing split into Self and Others. Enable damage and healing independently in Settings.
- Include non-attacking healers and damage recipients during tracked fights. Keep personal statistics limited to confirmed own/group players and received combat events.

## CatScratch v1.0.24

- Keep Your fights fully clickable and opaque when the DPS meter is locked or transparent.
- Give the history window its own solid background without changing the meter display settings.

## CatScratch v1.0.23

- Turn off automatic fight saving by default, including existing installations. It remains opt-in in Settings.
- Add Save last fight to the meter. Save the latest completed fight once, retaining only the last 10 saved fights.
- Add Delete saved fights in Settings with confirmation. Existing saves remain untouched until explicitly deleted.

## CatScratch v1.0.22

- Make compact CP/GS and total damage larger and semibold, with brighter text and sharper shadows.
- Increase compact row spacing to fit both lines and let text follow the row-size setting without becoming tiny.
- Improve compact RTT, party DPS and percentage readability while preserving the existing layout.

## CatScratch v1.0.21

- Small release to test the manual update flow and installation-local download folder.
- No changes to meter behavior, appearance or game-data handling.

## CatScratch v1.0.20

- Store update packages, extracted files and rollback backups in userdata/temp/updates inside the installation, instead of Windows temp on C:.
- Keep compatibility with older updaters for the migration update and retain cleanup of stale temporary files.

## CatScratch v1.0.19

- Check for updates only checks availability and shows the release notes.
- Download and installation start only after pressing Update now. Remove automatic background installation, including for existing settings.

## CatScratch v1.0.18

- Remove fixed class-color blocks behind compact names and stats so only the actual damage bar defines the colored length.
- Keep contrasting text inside the fill and light shadowed text outside, including short bars.
- Preserve damage shares and the existing relative-to-top bar setting.

## CatScratch v1.0.17

- Fix intermittent room-stat previews when the group roster arrives before complete character identity.
- Keep only the latest pending live roster and confirm it against the fresh character name and server.
- Preserve confirmed group stats and membership through dungeon/matchmaking loading. Reconfirm fresh actor IDs by character name and server before attributing teammate damage.
- Clear membership on leaving, group or character changes and capture reset; discard stale actor IDs on zone changes.
- Actual fight damage takes priority over pre-combat group stats.
- Recognize Global dungeon player records using the exact 64-bit character identity from the live roster when the short server field is absent. Verified against a private real dungeon capture with all five teammates contributing damage.

## CatScratch v1.0.16

- Add a Check for updates button directly to the DPS meter footer, including compact mode.
- Show checking feedback, prevent repeat clicks while checking and open the existing update window when a new version is available.

## CatScratch v1.0.15

- Fix confirmed party damage silently reverting to self-only tracking after 30 seconds without a roster update.
- Keep live confirmed membership until an explicit leave, membership change, identity invalidation, zone change or capture restart.
- Continue excluding unknown actors, other servers and unconfirmed summon owners.

## CatScratch v1.0.14

- Improve compact names, combat power and gear-score readability with automatic black or white text chosen for each class color.
- Keep a solid class-colored backing behind compact identity labels, including when a damage bar is short or displays are locked.
- Slightly increase compact gear text size.

## CatScratch v1.0.13

- Detailed fight statistics stay clickable even while the DPS meter and event displays are locked.
- Keep detailed statistics fully opaque with a solid dark background, independent of display opacity.

## CatScratch v1.0.12

- Assign the purple paw explicitly to all application windows and their native small and large Windows icons.
- Load the system-tray icon directly from the packaged resource instead of the Windows executable-icon cache.
- Show the paw in the custom title bars of secondary windows.

## CatScratch v1.0.11

- Copy full fight damage for every recorded player, with full integer values and percentage shares.
- Choose individual game-chat lines from the copy menu. Long rankings are split into short messages without omitting players.
- Add a chat-copy button to the meter footer. The same options are available in player menus and breakdowns, including saved fights.
- Export stays clipboard-only: paste and send yourself. No automatic game inputs or outgoing game messages are generated.
- Unknown-owner summons are not listed as teammates, and partial world-boss data remains identified as received damage.

## CatScratch v1.0.10

- New purple paw icon for the meter, executable, shortcuts, installer and system tray.
- Dungeon-room stats take priority while in a room. Incoming member updates and removals refresh names, classes, GS and CP.
- Leaving the room clears the room preview immediately and resumes the normal meter view. Entering the dungeon clears the pre-entry preview.
- Keep confirmed character information through partial updates. Fresh matching player announcements can rebind the character after zone loading without an additional teleport.
- Restarting capture retains known character metadata while requiring fresh actor proof; game exit and character conflicts invalidate live identity.
- All character reconciliation remains passive. Old actor IDs and other-server names cannot claim your damage.

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
