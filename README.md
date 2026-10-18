# CatScratch

A separate, always-on-top Windows DPS meter for **AION 2**, by Zerafithekitten. Track your own fights and confirmed party members in a compact dark gray interface with restrained purple accents and cat-paw branding.

[Download Stable](https://github.com/Zerafithekitten/CatScratch/releases/latest) · [Release history](https://github.com/Zerafithekitten/CatScratch/releases) · [Report a bug / request a feature](https://github.com/Zerafithekitten/CatScratch/issues/new/choose)

| Channel | Current version | What it includes |
| --- | --- | --- |
| **Stable** | **[v3.0.3](https://github.com/Zerafithekitten/CatScratch/releases/tag/v3.0.3)** | Independent control panel, reworked Pet tracker with five recommendations, custom colors and transparency, event timeline and favorites, effective healing and the complete tested Beta feature set. |
| **Beta** | [v3.0.4-beta.7](https://github.com/Zerafithekitten/CatScratch/releases/tag/v3.0.4-beta.7) | Adds custom Hidden cube alert audio and experimental monitor selection, alongside passive cube alerts and selectable Pet tracking modes. |

English is the default. German, Russian and French are also available. Screenshots use synthetic sample data; names and numbers are examples. All features described below are included in **Stable v3.0.3** unless a limitation is stated.

## Contents

- [Getting started](#getting-started)
- [CatScratch control panel](#catscratch-control-panel)
- [DPS meter and fight details](#dps-meter-and-fight-details)
- [Group-search statistics](#group-search-statistics)
- [Permanent boss personal bests](#permanent-boss-personal-bests)
- [Last played with](#last-played-with)
- [Death recap](#death-recap)
- [Interface colors](#interface-colors)
- [Overlay profiles](#overlay-profiles)
- [Boss fight comparison](#boss-fight-comparison)
- [Fight selection and saved fights](#fight-selection-and-saved-fights)
- [Pet tracker](#pet-tracker)
- [Events, boss timers and sounds](#events-boss-timers-and-sounds)
- [Updates and release channels](#updates-and-release-channels)
- [Patchnotes](#patchnotes)
- [Help & symbols](#help--symbols)
- [Controls and shortcuts](#controls-and-shortcuts)
- [Automatic start and close with AION 2](#automatic-start-and-close-with-aion-2)
- [Languages and stream mode](#languages-and-stream-mode)
- [Debug recordings and feedback](#debug-recordings-and-feedback)
- [Limitations](#limitations)
- [Development and license](#development-and-license)

## Getting started

1. Download **CatScratch-Setup.exe** or **CatScratch-win-x64.zip** from your preferred release channel. Extract the entire portable ZIP to a writable folder.
2. Install [Npcap](https://npcap.com/#download) separately if it is missing. CatScratch can open the official download page for you.
3. Start **CatScratch.exe** and accept the Windows administrator prompt. Cancelling the prompt prevents startup.
4. Teleport or change zones once while the meter is running so it receives fresh character information.
5. Use borderless windowed mode to keep the overlay visible above the game.

Both Setup and portable installations support in-app updates. Your settings and local records are kept in `userdata` beside the program. The installed version appears underneath the CatScratch title in the separate control panel, which holds the module and general controls.

## CatScratch control panel

The permanent control panel holds the version, single Settings gear and general controls independently of the DPS display. It stays clickable during global click-through. Its X is the only in-app Exit action; auxiliary module close buttons only close their own window.

Choose horizontal or compact vertical orientation, nine screen anchors or free placement under **Settings > Appearance > Control panel**. The vertical layout uses four tight symbol columns with subtle separators. Panel position, alignment, transparency and pin state are saved independently and included in overlay profiles. The DPS position pin stays on the DPS display itself.

You can hide the combat display under **Settings > Appearance > Modules > Show DPS meter** and continue using Pets, events and capture. Restore the display from the panel, tray or its shortcut. The panel remains your direct Settings access point.

<img src="docs/screenshots/control-panel-horizontal.png" alt="Stable CatScratch horizontal control panel with version and general controls" width="400" />

<img src="docs/screenshots/control-panel-vertical.png" alt="Compact vertical CatScratch control panel with four symbol columns" width="132" />

## DPS meter and fight details

The meter tracks your own damage while solo and confirmed current party members while grouped. Compact rows show class emblems, class-colored background fills, name, DPS, damage share, Combat Power (**CP**) and Gear Score (**GS**). Missing values appear as dashes. Incoming damage is shown separately in red.

The collapsible **Personal combat** section shows self healing and healing of others. An optional boss HP panel uses received health data. A small **Enrage time** bar appears only for a received deadline matching that boss; it represents the server boss time limit, not a predicted enrage mechanic. The control panel shows the Europe/Berlin clock and fight duration; the combat display footer retains party DPS and a passive RTT estimate.

**Effective self healing and healing of others.** Both personal-combat values count only HP gains confirmed by nearby health updates, excluding overhealing. Recognized direct group heals and healing-over-time ticks are supported. An asterisk marks an incomplete confirmed subtotal; a dash means healing occurred but none could be confirmed. Missing or ambiguous HP data, overlapping healers and concurrent damage are excluded rather than estimated. The tooltip explains the marker. Existing reported-healing fields in saved statistics and boss records retain their original meaning; the personal-combat display uses separate effective values.

<img src="docs/screenshots/meter.png" alt="Stable CatScratch combat display with class bars, CP/GS and an independent position pin" width="420" />

Automatic fight-end detection is available in the combat settings. Resize the meter down to 260 px, adjust opacity and UI scale (75–150%), or switch between compact and normal layouts. Click-through hides the frame and controls while keeping statistics readable. Settings remain clickable.

Open a player's fight details for DPS, skills, accuracy, rotation and defense, with sorting, a timeline and chat/image export.

<details><summary>Fight details and click-through previews</summary>

<img src="docs/screenshots/fight-details.png" alt="Fight details with player cards, skills and DPS timeline" width="1040" />

<img src="docs/screenshots/meter-click-through.png" alt="Click-through meter with transparent outer frame" width="420" />

</details>

## Group-search statistics

Before combat, the live meter can show the confirmed group-search room's members with their names, classes, CP and GS. No damage is required. An active fight takes priority, and completed fight results remain available through Statistics.

**Group-search reliability:** empty slots in the middle of a room no longer hide the players after them. New arrivals fill those slots without discarding the remaining members. Confirmed leave notifications clear the group even if the packet reader missed an earlier roster. CP/GS are kept separate for same-named characters on different servers, including after cache reloads.

<img src="docs/screenshots/group-room.png" alt="Group-search room overview with names, classes, CP and GS before combat" width="420" />

Group information must actually arrive from the game. CatScratch cannot reconstruct a roster or values it never received.

## Permanent boss personal bests

Open the **small trophy icon** in the CatScratch control panel. Search the bundled catalog of real dungeon bosses and world bosses, including bosses for which you have no record yet.

Each boss retains **six independent best values**, separately for each character, server and class:

| Statistic | Better result |
| --- | --- |
| DPS | Higher |
| Total damage | Higher |
| Strongest hit | Higher |
| Healing done | Higher |
| Own deaths | Lower |
| Damage taken | Lower |

Each value can come from a different qualifying victory. Equal or worse results never replace a best. Values and achievement dates persist across restarts and updates, independently of saved fights: **there is no ten-record limit for boss bests**.

A record requires a confirmed real boss kill, usable own-character data, a known starting HP baseline and boss-only outgoing damage. Test mode, recorded playback and incomplete or unqualified fights do not improve personal bests. Missing packets can prevent qualification or omit observed events.

<details><summary>Boss personal bests preview</summary>

<img src="docs/screenshots/boss-personal-bests.png" alt="Permanent boss personal bests with six independent statistics and searchable boss catalog" width="920" />

</details>

## Last played with

Open the **player icon beside the trophy in the control panel**. The local list keeps confirmed teammates, newest first, separately for each own character/server.

Compact rows show name, class, server, last-played time and the last received **CP / GS**, without expandable equipment panels. Every player has an individual **copy-name button**. Repeated appearances update an existing entry rather than adding duplicates. The list persists across restarts and has no ten-player limit.

<img src="docs/screenshots/last-played-with.png" alt="Recent teammates with compact CP/GS values and individual copy-name buttons" width="760" />

Unknown CP/GS remain dashes until usable values arrive; missing values do not erase known values. Stream mode masks displayed and copied names consistently. Test mode and recorded playback do not populate the permanent list.

## Death recap

The **tombstone icon** enables or disables the module. When your character dies, a compact recap shows only the five latest received hits from the previous **ten seconds**, newest first, with attackers, skills, time before death, red damage values and their matching total. It opens centered in the upper half of the meter screen, above the standard resurrection controls, and remains movable.

<img src="docs/screenshots/death-recap.png" alt="Death recap listing recent attackers, skills and incoming damage" width="400" />

It closes when fresh positive own HP or a map reload confirms the character is alive. Same-map resurrection and batched HP updates are supported. The default toggle is **Shift+F4**, configurable in Settings > Hotkeys. The recap is temporary and is not retained after restarting CatScratch.

## Interface colors

**Available in Stable.** Settings > Appearance > Interface colors lets you choose accent, background and text colors using a saturation/brightness field, a rainbow hue strip, swatches or #RRGGBB. The preview is a draft: Save applies the colors across open windows and keeps them for the next start; Closing with X discards later unsaved palette edits. Restore default colors returns to the gray/lavender CatScratch palette after Save. Low contrast is shown as advice and does not prevent saving a valid color choice. For a small Easter egg, the **?** beside Restore default colors enables **Mario Party!**: a continuous rainbow wallpaper scrolls across the general window backgrounds in a seamless 12-second loop after Save, including after restart. Restore default colors and Save stops the mode. Class, healing, damage, event-type and golden favorite colors keep their meaning; the purple paw stays unchanged.

<details><summary>Color settings and picker previews (Beta)</summary>

<img src="docs/screenshots/interface-colors.png" alt="Appearance settings using an example custom blue accent palette" width="760" />

<img src="docs/screenshots/color-picker.png" alt="Saturation and brightness field with a rainbow hue strip" width="304" />

</details>

## Overlay profiles

<img src="docs/screenshots/overlay-profiles.png" alt="Optional dark overlay profile settings" width="640" />

Use **Settings > Overlay profiles**. Arrange the modules first, enter a name and choose **Save new profile**. Use **Apply profile**, **Update selected** or **Delete selected** to manage layouts. Applying acts immediately and closes Settings, discarding any unsaved edits in other tabs.

Profiles retain meter and module placement, open states, position pins, UI scale, opacity, row size and display choices. Capture, update channel, hotkeys, autostart, event schedules and sounds stay unchanged. Profiles are optional and switch only when you apply one. Death recap keeps its automatic upper-center default.

## Boss fight comparison

<img src="docs/screenshots/boss-comparison.png" alt="Personal boss fight statistics side by side" width="580" />

Open a finished boss fight from Statistics or saved history and use **Boss fight comparison** in its statistics header. The last earlier matching recorded fight is compared automatically. Both run cards show the boss name prominently, with date and duration beneath; no manual fight list or technical identifiers. The DPS meter remains Live.

The table compares your duration, DPS, damage, healing, damage taken and deaths with signed differences (current minus previous). Matches require the same boss, map ID, character/server/class, outcome and damage mode. Possibly partial recordings are flagged and unknown deaths remain unknown. Difficulty variants without different map IDs cannot be distinguished.

Comparisons use existing session fights and the ten saved fights. Save boss fights to retain them after restarting; this feature does not turn on automatic saving or change permanent boss bests.

## Fight selection and saved fights

Use **Statistics** for the live fight, current-session fights and **All saved fights**. Selecting an earlier session or saved fight opens its detailed statistics immediately in a separate window, starting with your own character when available. The DPS meter stays on **Live** throughout; opening history never replaces its current fight or room stats. Repeated clicks on the same fight reuse its statistics window. Boss bests and recent teammates have their own buttons; they are not duplicated in Statistics or Feedback.

Saving is optional. Press the **save button after combat** to keep a full fight. The saved-fight history retains the **ten newest saved fights**. This limit applies to full fight records only, not permanent boss bests or recent teammates. Automatic saving is disabled by default.

<details><summary>Fight selection preview</summary>

<img src="docs/screenshots/fight-picker.png" alt="Statistics menu focused on live, session and saved fights" width="340" />

</details>

## Pet tracker

**Settings > Pets** contains the pet module toggle, width and collection/level-up sound option. Save applies changes while Settings stays open; closing with X discards later unsaved edits; the pet overlay has a position pin instead of a settings gear.

Use the **paw button in the control panel** to toggle the compact pet tracker. With confirmed collection data, the module recommends up to five incomplete pets in the current supported region without an attack. Freshly observed, confirmed source monsters take priority and mark their pets with **⌖**. Region recommendations cover Verteron, Altgard and Chaotic Lower Reshanta; completed pets are excluded. The compact list scrolls and shows soul progress toward the next level. If no supported region or nearby source is known, the last three personally attacked incomplete pets remain available as a fallback. Completed pets are hidden in every view and disappear as soon as their final soul loot confirms level 3. Soul loot updates the count after a kill.

Progress is saved per character. A **~** prefix marks last-known counts until fresh login data arrives. If no baseline is saved, log into the character once while CatScratch is running; opening an already cached pet menu may not resend the collection. Recent completed local recordings can help recover a missing baseline.

The bundled catalog contains confirmed Global monster-source mappings for 165 pets and locally stored community map locations for 156 matched pets, attributed to [AION 2 Pets / Wikily](https://aion2pets.app/map/). Recommendations use received map IDs and fresh monster announcements, not a scan of game memory. Map-pixel coordinates have not been validated against live player coordinates: no exact distances, navigation arrows or player radar are displayed. A regional recommendation means a recorded farming source in that region, not a guaranteed nearby spawn or soul drop. Region changes and capture resets clear old location context. Unknown collection progress stays unknown; **~** marks cached progress.

<details><summary>Pet tracker preview</summary>

<img src="docs/screenshots/pet-tracker-region.png" alt="Compact regional pet recommendations with nearby-source marker and soul counts" width="300" />

</details>

## Events, boss timers and sounds

Use the **single gear in the control panel > Events** for event visibility, region, selection, module widths, opacity, boss reminders and all event sounds. Event modules no longer have their own gear. Use Save to apply these options while Settings stays open; closing with X discards later unsaved edits; sound previews and custom audio selection stay inside the same Settings window.

Selected events and enabled world-boss timers within the next hour appear as compact movable cards. Both modules restore their own positions and open states.

The CatScratch control panel is the single taskbar entry; display modules do not add separate taskbar/Alt-Tab entries or native Windows Snap frame styles. Each display module has a matching position pin at the upper right. Pinning prevents dragging without changing button interaction or click-through. Pin states survive restarting and are stored with optional overlay profiles; unpin to move a module again.

**Upcoming and Started are separate.** At the start, an entry leaves Upcoming and moves into the movable **Started events** module. Enable that module in Settings > Events; drag its header or cards to place it, and use its X to close it. Click-through and show/hide all windows work for both modules.

| Started entry | Text | Visibility |
| --- | --- | --- |
| Event | **Open!** + remaining countdown | 3 minutes |
| Spacetime Rift | **Open!** + remaining countdown | 10 minutes |
| Boss | **Spawned**, no countdown | 2 minutes |

**24-hour timeline.** The clock in Upcoming opens a horizontal planner with a separate track for each event. It shows all scheduled event types in the selected region and known next world-boss spawns. Star an event to persist it as a favorite: its timeline markers and matching overlay cards receive a thin, softly glowing gold frame. Favorites do not change overlay selection or reminder sounds. Events are turquoise, PvP lavender and bosses orange. Times use the selected region, including daylight-saving changes. The planner refreshes automatically every minute while open and immediately when reopened. It uses the bundled community schedule, rather than fetching live schedule changes; event times are not officially confirmed.

<img src="docs/screenshots/event-timeline.png" alt="Compact 24-hour horizontal event timeline with separate event tracks and gold-framed favorites " width="1100" />

Expired entries disappear automatically. While an event is active, a later recurrence with the same name is held out of Upcoming; its next start returns after the active entry expires.

<img src="docs/screenshots/started-events.png" alt="Separate movable Started events module with Open countdowns" width="520" />

Boss timers are separate from personal boss bests. They use received in-game boss-list data and observations; estimated intervals remain identified as estimates.

Optional per-event and pet unlock/level-up sounds support preview and volume settings. Custom **WAV / MP3** files must be at most **10 seconds and 10 MB**. The chosen filename is displayed; **Change audio…** replaces it. Invalid selections show an inline error and keep the previous sound.

<details><summary>Events, boss timers and sound settings</summary>

<img src="docs/screenshots/upcoming-events.png" alt="Selected upcoming event cards and countdowns" width="520" />

<img src="docs/screenshots/boss-timers.png" alt="Observed boss timers" width="760" />

<img src="docs/screenshots/events-settings.png" alt="Central event preferences in the single Settings window" width="760" />

</details>

## Target screen (Beta, experimental)

Under **Settings > Appearance > Target screen (experimental)**, choose the monitor for CatScratch. **Save** moves the control panel and open overlays to it. New modules and Settings also open there; windows remain freely draggable. The choice survives restart and is independent of overlay profiles. **Automatic (saved positions)** keeps existing placements; an unavailable chosen monitor falls back to the primary screen.

## Hidden cubes (Beta)

**Beta only:** enable **Settings > Hidden cubes > Enable hidden cube alerts (experimental)** to show a compact, movable **HIDDEN CUBE DETECTED** warning for the cube type confirmed in two recordings. The module is off by default. It supports an independent pin, click-through, saved position and overlay profiles. The warning uses bold red text with gently pulsing exclamation marks. **Show test alert** lets you position and pin a labeled preview even without a detected cube; closing Settings also closes the preview.

Choose an optional **MP3 or WAV** in the same tab, adjust its independent **Volume**, and use **Preview** to listen before saving. One notification plays for each newly detected cube, rather than every repeated announcement. Visual test alerts stay silent. Files must be at most **10 seconds / 10 MB** and remain at their selected location.

Opening the cube or changing context clears the warning; announcements also expire after 60 seconds without a refresh. An announcement confirms that the server sent the object, rather than a measured distance or continued visual presence. Other cube variants, map placement, direction and distance remain unverified. No screenshot capture or game-memory access is used.

## Updates and release channels

Choose **Stable** or **Beta** in **Settings > Updates**. Stable is the default download; beta releases are marked as prereleases and do not replace it. Switching channels may offer an older version when returning to Stable.

With automatic updates enabled, CatScratch checks shortly after startup and **every ten minutes**. It downloads, verifies and extracts an available update in the background while the current meter keeps running.

The small green **Update!** notice appears **only after the update is fully prepared**, in a separate popup above the control panel. It follows and hides with the panel, without changing the panel position or dimensions. Click it to apply the prepared files and restart; CatScratch does not restart automatically.

<img src="docs/screenshots/update-notice.png" alt="Small green Update! button shown separately above the control panel" width="72" />

After a successful new startup, the old backup, staged payload and downloaded ZIP are removed. Failed updates retain recovery files. Settings, saved fights, personal bests, recent players and captures are preserved. Update staging stays under `userdata/temp/updates`; a writable program folder is required.

When a Beta is promoted and its Stable build starts, Beta subscribers automatically return to the Stable update channel. A later deliberate switch back to Beta is respected until the next Stable transition. The installed version then shows the Stable number, currently **3.0.3** without a beta suffix.

Use **Check now** in Settings or **Check for updates** in the tray menu for a manual check. Automatic checking and preparation can be disabled in Settings. CatScratch also reduces data-loading allocations and moves optional startup work behind the first meter display.

## Patchnotes

Open **Patch notes** from the control panel or Settings. Versions are listed **on the left**, newest first. Each version shows only its own changes, with a **clickable contents overview** above detailed explanations. Repeated older notes in cumulative release bodies are omitted from that version's view.

**The last ten versions are available.** The version sidebar is scrollable when space is limited. Stable shows Stable releases; the Beta channel also includes beta versions. Bundled notes work offline; **Refresh from GitHub** updates the history.

<img src="docs/screenshots/patch-notes.png" alt="Ten patchnotes versions in a left sidebar with clickable contents and individual release notes" width="580" />

[Read all published release notes](https://github.com/Zerafithekitten/CatScratch/releases).

## Help & symbols

Settings > Help & symbols explains the panel and module icons, their functions and states, using your saved shortcuts. The guide is read-only and available in English and German.

Settings > Appearance > Control panel offers compact horizontal and vertical layouts, nine screen anchors and free placement. The panel can reach the full screen edge and follows display/background opacity. Dragging switches to free placement. Save applies changes while Settings stays open; closing with X discards later unsaved edits. Layout and alignment are included in overlay profiles.

## Controls and shortcuts

**Optional DPS display.** Settings > Appearance > Modules > Show DPS meter hides combat statistics while capture and other modules continue. The permanent CatScratch control panel contains the version and general buttons, keeps Settings directly available, and provides a DPS display toggle even with global click-through enabled. Its X is the only in-app Exit action. Panel and meter have independently saved placement and pins. The tray icon and configured DPS meter shortcut (Shift+F5 by default) also restore the meter. The choice persists across restarts and is included in overlay profiles. Automatic visibility and Show/hide all respect a disabled display.

The single control-panel gear opens central Settings. Updates and Feedback have separate tabs; General contains test mode, language and the optional game companion. Event and pet options apply with Save while Settings stays open; close with X to discard later unsaved edits.

Record a combination in **Settings > Hotkeys** and save. Defaults:

| Action | Shortcut |
| --- | --- |
| Toggle death recap | **Shift+F4** |
| Show/hide DPS meter | **Shift+F5** |
| Toggle Personal combat | **Shift+F6** |
| Upcoming events | **Shift+F7** |
| Boss timers | **Shift+F8** |
| Pet tracker | **Shift+F9** |
| Show/hide all windows | **Shift+F10** |
| Toggle click-through | **Shift+F11** |
| Restart meter measurement | **Shift+F12** |

The all-window shortcut restores the previously open windows and their positions. App shortcuts pause while Settings is active. The main window's **X exits the entire program**; show/hide shortcuts only change visibility. The tray menu remains available in click-through mode.

<details><summary>Settings preview</summary>

The Settings sidebar groups related pages under **General & appearance**, **Game modules**, **Controls & data** and **Updates & help**, with clear headings and dividing lines. The topic order stays consistent in all four languages.

<img src="docs/screenshots/settings.png" alt="Current CatScratch settings in the meter's dark gray and purple style" width="760" />

<img src="docs/screenshots/updates-settings.png" alt="Independent Updates settings tab" width="760" />

<img src="docs/screenshots/feedback-settings.png" alt="Independent Feedback settings tab" width="760" />

</details>

## Automatic start and close with AION 2

**Purpose:** CatScratch can be ready whenever you play, without remembering to open it first. When the game closes, the meter closes too. Only a small game watcher stays idle between sessions; the full meter and packet capture do not need to stay running all day.

### Enable or disable it

In **Settings > General > AION 2 companion**, enable **Start and close CatScratch with AION 2**, select the actual **game EXE** and save. This is a single combined option and is **off by default**. Steam, Purple and shortcuts work because detection follows the selected game executable, independently of the launcher.

When enabled, a Windows Task Scheduler entry named `CatScratch-Game-…` starts the watcher at Windows sign-in. It also starts immediately when you save the option. The watcher checks every two seconds, opens CatScratch once per game session and CatScratch closes after ten seconds without the game. Brief game restarts reset that grace period. If you manually quit the meter, it stays closed until the next game session.

Turn the option off and save to remove the scheduled task and let the watcher exit. This does not close the game. Uninstall also removes the registration. No watcher is registered while this option is off.

### Why PowerShell is used

PowerShell provides the small Windows background check that remains available while CatScratch itself is closed. Its job is to detect the selected game and launch the meter. The DPS meter, its interface and combat calculations still run in CatScratch itself.

This keeps the watcher separate from the program files that an update replaces, so it does not lock the running meter's binaries or prevent background update preparation. It also avoids installing a permanent Windows service or requiring the entire meter and capture engine to run while you are not playing.

### What runs and what it checks

- The local watcher checks process names, executable paths and the current Windows session, and checks whether the meter is already running. It never starts or stops AION 2 itself.
- The watcher does not capture game traffic, download updates, upload files, read chat or ask for account credentials. Network capture and normal update checks belong to the meter after it starts.
- The task runs for the signed-in user with the highest available privileges, matching CatScratch's existing administrator requirement. It does not create a SYSTEM service or store your Windows password.
- The shipped local script uses `-ExecutionPolicy Bypass` for its own PowerShell process. CatScratch does not change the machine-wide PowerShell execution policy. Managed Windows restrictions may still prevent it from running; failed setup is reported rather than silently enabling the option.
- Scripts and configuration are kept locally in `userdata/game-start`. You can inspect the [watcher source](https://github.com/Zerafithekitten/CatScratch/blob/v3.0.1/src/AionMeter.App/Services/GameLifetime.cs) and [windowless launcher source](https://github.com/Zerafithekitten/CatScratch/blob/v3.0.1/src/AionMeter.App/Launcher/GameWatcherLauncher.cs).

### Why no PowerShell window should appear

CatScratch uses a tiny windowless starter that creates PowerShell with **CreateNoWindow**. The watcher remains in the background without allocating a console window, including the brief console flash possible with the previous direct PowerShell startup. Existing enabled installations switch to the quiet task when the updated meter first starts. Automated checks verify that the watcher has no console window.

## Languages and stream mode

Select **English, Deutsch, Русский or Français** in **Settings > General > Language**. Skill and monster names use bundled language tables, with English fallback where translations are missing. Patchnotes remain in English.

**Stream mode** replaces displayed and copied player names with consistent aliases such as Cat 1, Cat 2 and Cat 3. Original names remain in local saved records and packet captures; stream mode does not anonymize those files.

Test mode provides isolated sample fights and pet progress. Test fights cannot be saved as real fights and do not improve permanent boss records or add recent teammates.

## Debug recordings and feedback

Enable **Recent debug buffer** in **Settings > Capture & data**. **Save recent debug** exports up to the preceding **60 seconds** to `userdata/captures`. The buffer is disabled by default and capped at 32 MB of accounted packet storage; heavy traffic can shorten the available time. Continuous packet recording is also available. **Open captures folder** opens the recording directory directly from this section, creating it if needed.

Only packets received while recording is enabled can be saved. For group issues, include the actual join/leave; for resurrection issues, include death and revival. Short captures can lack earlier character or protocol context. Captures can contain character names; keep them private. Nothing is uploaded automatically.

Use **Report a bug / request a feature** in Settings > Feedback, or [open an issue](https://github.com/Zerafithekitten/CatScratch/issues/new/choose). Reports must be in **English**; a GitHub account is required. Please include the version/channel, what happened and how to reproduce it. Feedback opens GitHub without sending logs or captures automatically.

<details><summary>Capture and recording settings</summary>

<img src="docs/screenshots/capture-settings.png" alt="Central capture settings with recent debug recording and Open captures folder" width="760" />

</details>

## Limitations

- Pet suggestions use sight and supported regions, not measured proximity. Missing map or collection data remains unknown; the tracker does not read game memory or provide exact own-player distances.
- Missing or unreceived server data can leave names, CP/GS, damage or deaths incomplete. The meter filters results to your confirmed character and party, rather than displaying every nearby player.
- Death recap needs fresh health or map data to confirm revival. Two recorded resurrection paths have been tested; a third method still lacks a supplied recording.
- RTT is a passive TCP estimate. Delayed acknowledgements and local accelerators can affect it or expose only a proxy connection.
- Event schedules are bundled community data from [Shugo.GG](https://shugo.gg/timers), not an official confirmation of Global event times. Verify availability in game.
- CatScratch passively captures traffic. It does not implement game-memory access, injection, game-file changes, outgoing game packets or simulated game input. This does not imply publisher approval.

## Development and license

Requires **.NET 10**. See [GITHUB-RELEASES.md](GITHUB-RELEASES.md) for release builds and [INSTALL.txt](INSTALL.txt) for installation and controls. Version tags build and test the Windows app and installer. Packet recordings and userdata are excluded from Git and release archives.

**GPL-3.0**, based on [cyberbadger6969/aion2-dps-meter](https://github.com/cyberbadger6969/aion2-dps-meter), base revision `414de5c1d5f3d45ce35701bd25229c2dba736bb6`. Keep [LICENSE](LICENSE) and [data/NOTICE.md](data/NOTICE.md) with redistributions. Full source accompanies releases. AION 2 belongs to NCSOFT.
