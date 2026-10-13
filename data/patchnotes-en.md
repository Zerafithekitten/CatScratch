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
