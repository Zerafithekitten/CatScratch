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
