## CatScratch v4.0.2

[Deutsche Patchnotes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.2/data/patchnotes-de.md)

### 🐾 A little extra cat

I've been putting together a small thank-you for anyone who wants to support CatScratch: a few designs, a few sounds and a golden paw. The useful parts of the app stay available to everyone. This is just a little extra cat for those who'd like one.

This update also gives the Windows audio connection a little more patience after a report of a valid MP3 being rejected.

<!-- contents -->
### At a glance
- [A new home for Patreon](#patreon)
- [First Paws: designs, sounds and a supporter paw](#first-paws)
- [How the packages work](#packages)
- [Custom MP3 and WAV sounds](#custom-audio)
<!-- /contents -->

<a id="patreon"></a>
### 💜 A new home for Patreon

Settings now has a **Patreon** tab, grouped between Updates and Feedback. It links to [my Patreon page](https://www.patreon.com/cw/deuzear_yuma) and lets you import a downloaded CatScratch supporter package directly.

I'll also use Patreon for development insights and occasional polls. Those live on Patreon; you don't need another account inside CatScratch.

![Patreon settings with the optional First Paws pack installed](https://raw.githubusercontent.com/Zerafithekitten/CatScratch/v4.0.2/docs/screenshots/patreon.png)

<a id="first-paws"></a>
### ✨ First Paws

The first separately distributed supporter pack contains:

- **Three designs:** Midnight Cat, Moonlight and Golden Paw.
- **Three original sounds:** Paw Chime, Moon Ping and Soft Purr.
- **An optional golden paw** in the existing control panel. Same size, same space, and you can switch it off whenever you like.

Choose a design or assign a sound in the Patreon tab, then press **Save**. Sounds can be assigned to Hidden Cubes, the Pet tracker and Resource warnings; installed sounds also appear in the event sound picker after reopening Settings. Your existing sound enablement and volume are kept.

<a id="packages"></a>
### 📦 Download, import, keep

Download the member package separately, extract its outer ZIP and select the **.cspack** file under Settings → Patreon → Import package. Leave the .cspack itself intact.

- Import installs content immediately. Colors, sound assignments and the paw apply when you save your choices.
- Packages contain signed designs and audio, with no executable plugins. CatScratch checks their contents before importing them.
- Everything works offline after import. There's no Patreon sign-in, subscription check or automatic download.
- Received packages remain usable after a membership ends. The signature verifies who issued a pack; it isn't a membership lock.
- Existing public features, custom colors, custom sounds and fixes remain available to everyone. Importing a pack doesn't replace your settings on its own.

<a id="custom-audio"></a>
### 🔊 A little more patience with custom sounds

Windows can announce an opened audio file before its duration is ready. CatScratch now waits briefly for that information and retries once with a fresh decoder after a validation failure. Playback also retries one decoder failure, so a temporary failure has a chance to recover before the next notification.

The reported **ALARM ALARM.mp3** is valid: it passed repeated checks here at about **4.10 seconds**, and Windows completed playback. I couldn't reproduce the original intermittent rejection, so I won't call its cause confirmed. Invalid files and sounds longer than ten seconds are still rejected, and a failed selection keeps your previous sound.

Thank you for supporting this one-person cat project, whether that's through Patreon, a useful bug report or testing an awkward little edge case with me. 💜

## CatScratch v4.0.1

[Deutsche Patchnotes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.1/data/patchnotes-de.md)

A small hotfix before the next adventure. Resetting the measurement should give you a fresh fight, not make CatScratch forget who's in your party. That was exactly what could happen.

<!-- contents -->
### At a glance
- [Damage after resetting the meter](#meter-reset)
- [What this fixes — and what is still being investigated](#investigation)
<!-- /contents -->

<a id="meter-reset"></a>
### 🛠️ Damage after resetting the meter

- A manual measurement restart now retains the already confirmed character and party for the current game session. New damage can be counted without waiting for the game to resend names and the party roster.
- The fight still resets and capture reconnects as before. Existing resource balances become approximate until confirmed; their ongoing storage remains available.
- Real logins, character changes and game exits still invalidate old session information. A fresh login table also clears old party proof during a quick switch on the same map.
- Capture restarts caused by changing capture settings still require fresh confirmation.

<a id="investigation"></a>
### 🔎 What is still being investigated

This fixes the confirmed restart-related problem. A separate report of a blank boss display before resetting is still open: the supplied recording starts after the reset, so it cannot establish that earlier cause. If it happens again, please save the recent recording immediately, before resetting; it contains the last minute.

Thanks for reporting it and helping me separate the two issues. One less place for the cat to lose the thread. 🐾

## CatScratch v4.0.0

[Deutsche Patchnotes](https://github.com/Zerafithekitten/CatScratch/blob/v4.0.0/data/patchnotes-de.md)

### 🐾 A little more cat, a lot more overview

This one grew a little. What started as “could I see my resources?” turned into two new modules, a character overview and a few helpers I now wouldn't want to play without.

I've also spent this round chasing the less charming habits: windows shuffling around when locked, values disappearing, and quick character switches confusing whose pockets we were looking into.

Thank you for the reports, recordings and patience through all those Betas. There's one person — and one cat fursona — behind CatScratch. Your testing helped me catch things I couldn't have found on my own. 💜

<!-- contents -->
### At a glance

- [Resources: your energy and currencies](#resources)
- [Dungeon entries: what's left to run?](#dungeons)
- [Character overview: check your alts](#characters)
- [Low-balance warnings and sounds](#warnings)
- [How fresh are those numbers?](#data)
- [World bosses, your way](#bosses)
- [Tidier windows and optional snapping](#windows)
- [Click-through and staying on top](#clickthrough)
- [Hidden Cubes, now with a little meow](#cubes)
- [The smaller fixes — and a better welcome after updates](#polish)
<!-- /contents -->

<a id="resources"></a>
### 💰 Resources: your energy and currencies

A separate Resources module brings these balances together:

- Odyle Energy
- Abyss Points
- Centuryroot Token
- Deava Pass Shop Coin
- Phantasmal Fragment
- Oath Coins

Choose each resource independently under Settings → Resources. Pick a vertical list, a compact horizontal strip or a two-column grid, with the matching game icons.

Odyle keeps regular and supplementary Energy separate. Confirmed regeneration, item use and currency changes update the display as their game messages arrive, including supported spending updates. There is no need to relog for every change.

Your selection, layout, position and pin survive a restart. No guessed recharge countdowns are added.

<a id="dungeons"></a>
### 🎟️ Dungeon entries: what's left to run?

Daily Dungeon, Nightmare and Shugo Festival now have their own optional module under Settings → Dungeon entries. Enable just the ones you want and choose List, Horizontal or Grid.

Each entry shows the regular count, additional entries in brackets and the capacity reported by the game. The module has its own saved position and pin and works with layout profiles and click-through.

Counts update when the game confirms consumption. Depending on the dungeon, that can happen at completion or when claiming a reward. Walking into a zone alone won't deduct an entry, and missing messages aren't guessed into a new balance.

<a id="characters"></a>
### 👤 Character overview: check your alts

Wondering what you left on another character? The character-card button beside the history tools opens a table of saved resources and Dungeon entries, separated by character and server. You can also open it from Resources settings.

The window now matches Last played with, with clear row and column dividers and centered values. Hover over a value to see its own last confirmation. Stream mode masks names here too.

Quick switching has also been fixed: login balances can arrive before the new character's name. CatScratch now holds those messages until it knows who they belong to, rather than putting them in the previous character's pockets. Similar names such as Zerafi and Zerafí stay separate.

If an earlier Beta saved mixed-up balances, logging into each affected character again after this update refreshes their records. Offline characters show saved information; CatScratch doesn't query them in the background.

<a id="warnings"></a>
### 🔔 Low-balance warnings and sounds

Set an optional threshold for each resource or dungeon, with a choice of bundled warning sounds and volume.

- Odyle checks regular available Energy.
- Dungeons check regular plus additional entries.
- Only freshly confirmed values trigger a warning.
- A low balance warns once. It rearms after recovering above the threshold and dropping again.

Everything is opt-in. If you haven't enabled a warning, the cat stays quiet.

<a id="data"></a>
### 🕒 How fresh are those numbers?

A normal value is confirmed from received game data. A value marked ≈ is the last known balance, waiting for fresh confirmation. Relog! means no usable baseline has been recorded for that value yet.

Tooltips now show when each individual value was last confirmed and how old it is. Older saved records without a timestamp say so rather than inventing one.

Known balances are retained as approximate during capture restarts. Test mode no longer clears real resource data. Character and server identities keep saved records separate, and fresh messages replace the corresponding cached values.

A small status dot in the control panel adds a tooltip distinguishing incoming game data, waiting for character data and capture errors.

<a id="bosses"></a>
### 👑 World bosses, your way

Settings → Events now includes a searchable list of known world bosses. Choose individual bosses, mark favorites and give each one a built-in or custom WAV/MP3 notification sound, with reminder lead time and volume.

Selected bosses and favorites remain visible beyond the normal one-hour window. Favorites have the familiar gold frame; confirmed living bosses appear in Started events. Unknown spawn times stay visibly unknown.

Watching is manual: detecting or recording a kill still saves the kill time and respawn timer, but no longer switches on that boss's watch bell for you. Existing selections are kept.

<a id="windows"></a>
### 📐 Tidier windows and optional snapping

Upcoming and Started events can anchor to the top or bottom of the screen when dragged near an edge. At the bottom, extra rows grow upward and shrink back down as they disappear. At the top, the upper edge stays put. Drag away to release the anchor; saved placements and profiles remember your choice.

Optional snapping under Settings → Appearance helps align modules with screen edges and other visible modules. Choose the gap yourself, or hold Alt while dragging to bypass it. Pinned modules remain fixed.

Profiles include snapping preferences without overwriting resource selections or warning thresholds.

<a id="clickthrough"></a>
### 🔒 Click-through and staying on top

The startup option is now called Start with click-through enabled. Enable it and Save to start locked even if you temporarily unlocked the overlays before closing.

Opening Upcoming or Started events no longer silently unlocks everything. Their cards, the Pet tracker and Hidden Cube display keep their coordinates when controls disappear, including repeated lock/unlock changes. The control panel stays clickable so you can unlock again.

I've also strengthened foreground recovery after window and display changes, with an independent periodic check that preserves position, transparency and the game's keyboard focus.

<a id="cubes"></a>
### 🐱 Hidden Cubes, now with a little meow

The additional Cube variant confirmed on the enemy side of Verteron is recognized. The alert background is smaller and sits closer around its text; locking the overlay no longer moves it.

New installations use the selected kitty meow as their default Cube sound. Existing sound choices, volume and enabled/disabled preferences stay yours. I couldn't resist a small meow for new arrivals.

<a id="polish"></a>
### 🛠️ The smaller fixes — and a better welcome after updates

- Incomplete character records and partial inventory messages no longer erase unrelated known balances.
- Odyle values of 128 and above decode correctly, along with accompanying Dungeon entries.
- Confirmed consumption is applied as an absolute count, avoiding double subtraction.
- Resource and Dungeon modules avoid rebuilding unchanged rows unnecessarily.
- Icons, numbers and Relog! labels are aligned more neatly; large balances fit narrow tiles better.
- Old snapshot-only currency stars are gone for the supported live-updated currencies. Approximate saved values use ≈.
- Help now reflects Save keeping Settings open and X discarding later unsaved changes.
- Patch notes use this more personal style from now on. German app language gets German notes; all other languages get English, with English fallback for older releases without a translation.
- After upgrading to a Stable release, its patch notes open once on the next start. Normal restarts and Beta updates don't keep bringing them back. They remain available from the panel whenever you want another look.

Everything from the 3.0.12 Beta series is included here, together with the final character-switch and table fixes. CatScratch light remains its separate edition.

Thanks for sticking your paws into all those test builds. Have fun with 4.0.0 — and may the next Cube be loud enough. 🐾

## CatScratch v3.0.12-beta.22

- Resource and Dungeon entry tooltips now show when each individual value was last confirmed, including its age. Approximate cached values retain their original confirmation time; older cache records explicitly show an unknown confirmation date.
- Added a Character resources overview with saved resource balances and Dungeon entries grouped by character and server. Open it from Resources settings or the new compact character-card button beside the history tools in the control panel. Saved values remain approximate until confirmed, and stream mode masks character names.
- Added independent, opt-in low-balance thresholds for each resource and dungeon, with optional bundled warning sounds and volume control. Odyle checks regular Energy; dungeons check regular plus additional entries. Only confirmed values trigger warnings, once per low-balance period and character; recovery rearms the warning.
- Added optional module snapping under Appearance with adjustable spacing. Modules align to screen edges and other visible modules after dragging; hold Alt to bypass snapping. Pinned modules retain their positions, and layout profiles include the snapping preferences without replacing warning rules.
- Added a compact capture-status indicator in the existing control panel, distinguishing incoming game data, waiting for character data and capture errors in its tooltip. The new overview icon has a unique character-card shape and matches the other button sizes in all three panel layouts.
- Preserves existing selections, saved placements and click-through behavior. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.21

- Added character-and-server-specific last-known storage for resources, Odyle Energy and Dungeon entries. Restored values show ≈ until fresh server messages confirm or replace them. Existing unknown values still show Relog! when no baseline has been recorded.
- Capture restarts retain existing values as approximate, and Test mode no longer erases live resources. Disk restoration requires a matching freshly identified character; confirmed character changes clear the previous display.
- Confirmed dungeon consumption replaces the stored absolute count without double subtraction. A zone change alone does not spend entries; missing consumption messages cannot be inferred. Login values arriving before a new character identity are held for the correct character.
- Preserves all layouts, pinning, click-through and resource selections. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.20

- Fixed available Odyle Energy values of 128 or higher being rejected: the shared property table uses unsigned variable-length numbers, not a single byte. Verified the supplied second-character login recording as 160 (+270).
- Applied the same property-format correction to Dungeon entries so a multi-byte Odyle value no longer invalidates the accompanying dungeon snapshot. The fix is independent of character/account and preserves existing settings.
- Added boundary coverage for single- and multi-byte balances. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.19

- Dungeon entries now offers List (vertical), Horizontal (compact) and Grid (two columns) under Settings > Dungeon entries. Choose the visible dungeons independently and adjust window width for List/Grid; Horizontal fits automatically and keeps full names in tooltips.
- Layout and width persist across restart and participate in overlay profiles without replacing dungeon selections. Preserves pinning, live updates, compact alignment and click-through geometry in every layout. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.18

- Added confirmed live balance updates for Deava Pass Shop Coin, including spending. Removed its snapshot-only * marker; all supported currencies now apply confirmed change messages immediately.
- Centered resource icons and balances/Relog warnings together in Horizontal layout, with symmetric outer spacing and a consistent gap. Raised text slightly for optical alignment with the icons.
- Preserves compact tile dimensions, click-through placement and existing settings. Stable and Light are unchanged. Beta.17 was held before publication; its approved alignment fix is included here.

## CatScratch v3.0.12-beta.16

- Refined Resources grid alignment: equal tile heights and full large balances scaled to fit narrow tiles. Resources and Dungeon entries use layout rounding, and dungeon labels/values are vertically centered.
- Removed unnecessary full row rebuilds in Resources and Dungeon entries when displayed values and preferences are unchanged. Live changes, selections, layout, width and language still refresh immediately.
- Corrected Help to reflect Save keeping Settings open and X discarding later unsaved changes.
- Completed the full review: 425 tests, all 16 native check groups, color persistence across restart, and visual checks of module/settings layouts including four languages and enlarged control panels. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.15

- Added Oath Coins as an independently selectable currency under Settings > Resources, with the original game icon in all three layouts. Existing resource selections are preserved; enable Oath Coins manually on existing installations.
- Confirmed Oath Coin change messages update the absolute balance live, including spending, without a relog or duplicate accumulation. Missing data still shows Relog!; character and capture resets clear old balances.
- Stable and Light are unchanged.

## CatScratch v3.0.12-beta.14

- Removed the misleading * marker from live-updated Abyss Points, Centuryroot Token and Phantasmal Fragment balances. Only Deava Pass Shop Coin keeps the last-received snapshot marker. Updated the corresponding tooltips and Settings explanation.
- Stable and Light are unchanged.

## CatScratch v3.0.12-beta.13

- Added an optional standalone Dungeon entries module under Settings > Dungeon entries. Independently select Daily Dungeon, Nightmare and Shugo Festival. Shows available (+extra) / server-reported capacity, with live confirmed consumption updates.
- Includes independent pinning, saved position, global visibility, layout profiles and click-through without shifting rows. Missing data shows Relog!; unknown capacity stays unknown until a snapshot arrives. Character and capture changes clear old counts.
- Resources now apply confirmed live balance changes for Abyss Points, Centuryroot Token and Phantasmal Fragment without requiring a relog. Odyle keeps its existing live updates; Deava Pass Shop Coin remains a last-received snapshot until change packets are verified. The * marker still means last received balance.
- Removed an orphan Resources window allocation when opening Checklist. Stable and Light remain unchanged.

## CatScratch v3.0.12-beta.12

- Fixed received resource balances being erased by incomplete character records or later server identification during login. Resolved own-player identity now reaches resource tracking consistently.
- Partial and unrelated inventory messages preserve other known resources; ambiguous duplicate entries invalidate only the affected resource. Confirmed character changes and capture resets still clear old values.
- Checked against the supplied login recording and added regression coverage. No active game-server queries, new currency-change decoders or instance-entry module are included. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.11

- Missing resource data now shows an orange **Relog!** warning instead of a dash in List, Horizontal and Grid layouts. The tooltip explains returning to character selection and logging in again.
- Received data replaces the warning. Confirmed zero balances remain visible as zero. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.10

- Added matching game icons for all five resources, in both the module and Settings. Icons are bundled for offline display.
- Choose List, Horizontal (compact icons and values) or Grid (two columns) under Settings > Resources. Names remain in tooltips in Horizontal mode; its width fits automatically.
- Layout selection applies with Save, persists across restart and participates in overlay profiles. Click-through retains layout geometry in all three variants. Currency * markers still mean last received inventory balance. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.9

- Added an optional standalone Resources module under Settings > Resources. Individually select Odyle Energy, Abyss Points, Centuryroot Token, Deava Pass Shop Coin and Phantasmal Fragment; adjust width and save the selection.
- Odyle displays available and supplementary energy separately and updates from confirmed login, regeneration and item-use packets. No recharge countdown or capacity is guessed.
- The four currencies display last received, unambiguous inventory balances, marked with *. Live currency-change tracking is not yet verified; relog to obtain a fresh inventory snapshot. Missing/ambiguous values remain unknown.
- Includes pinning, click-through without layout shifts, global visibility, saved placement and layout profiles. Character/capture changes discard old values. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.8

- Hidden cube alerts use a narrower, compact background directly behind the warning text. The hidden header area stays transparent in click-through mode without shifting the alert.
- Preserves animated red warning markers, sound preferences and saved placement. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.7

- Upcoming and Started event modules automatically anchor to the upper or lower screen edge when dragged close to it. At the bottom, extra events expand upwards and disappearing rows shrink back down; at the top, the upper edge stays fixed.
- Dragging away releases the edge anchor. Saved placements and overlay profiles retain the edge choice; freely positioned modules keep their existing behavior.
- Includes checks for both modules, dynamic growth/shrink and repeated click-through toggles. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.6

- Upcoming and Started event cards retain their screen position when click-through hides their controls, including subsequent refreshes and repeated toggles.
- Applied the same layout-preserving fix to the Pet tracker, Hidden cube alarm and shared module title bar. Hiding borders also retains their layout space in the alarm and death recap.
- New installations use the owner-selected kitty meow recording for Hidden cube alerts. Existing saved sound choices, volume and enabled/disabled state stay unchanged.
- Added repeated lock/unlock geometry checks. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.4

- Hidden cube alerts now recognize the additional variant confirmed in an enemy-side Verteron approach/open recording. The existing alarm and custom sound are reused; opening the cube clears the alert.
- Both earlier cube recordings still pass. Other unverified variants remain unsupported. Stable and Light are unchanged.

## CatScratch v3.0.12-beta.3

- World bosses are watched only when you explicitly enable their bell. Detected kills and manual kill entries still save kill times and respawn timers without subscribing you automatically. Existing watch selections are preserved.

## CatScratch v3.0.12-beta.2

### Overlay visibility and click-through fixes

- Topmost recovery now runs independently of the meter's background refresh, with prompt recovery after window deactivation or display changes and a periodic native refresh. It preserves position, size, click-through and keyboard focus, skips hidden/minimized windows, and respects active CatScratch dialogs. This hardens recovery; confirmation in the reported in-game situation is still needed.
- The former Remember click-through option is now clearly labeled **Start with click-through enabled**. When enabled, startup always enables the mode, even if a temporary unlock was saved previously. Existing enabled preferences are retained. The DPS window also keeps its native no-activation flag and transparent appearance.
- Opening Upcoming events or Started events no longer silently disables global click-through. Both modules inherit the chosen state; the control panel remains clickable for explicit unlocking.
- Includes all Beta 1 world boss features. Stable remains 3.0.11; CatScratch light is unchanged.

## CatScratch v3.0.12-beta.1

### Individual world bosses

- Settings > Events now offers a searchable list of known world bosses with individual display choices and favorites.
- Selected bosses and favorites stay in the event modules beyond the normal one-hour window. Unknown spawn times are labeled honestly; favorites receive the existing glowing gold frame. Confirmed living bosses stay in Started events.
- Each boss can use its own built-in or custom WAV/MP3 sound, with event reminder lead time and volume. Reminders are issued once per known respawn and use only the current server's timer data.
- Choices, favorites and sounds apply on Save and survive restart. Existing bulk boss display, generic events and watched-boss sounds remain available.
- Beta only; Stable remains 3.0.11. CatScratch light is unchanged.

## CatScratch v3.0.11

### OPTIONAL CLICK-THROUGH RESTORATION AFTER RESTART

- Added **Settings > Appearance > Modules > Remember click-through after restart**. Enable it and Save to restore the last saved click-through state when CatScratch starts.
- Previously startup always switched click-through off, even though toggling the mode saved its state. With this option enabled, a locked session starts locked again; an unlocked session starts unlocked. The option does not force click-through on immediately or override a later deliberate unlock.
- The permanent control panel remains clickable, including Settings and the lock toggle. You can still unlock using the panel, the configured hotkey or the tray controls. Auxiliary overlays inherit the restored state as they open.
- The option is off by default to retain the previous startup behavior unless explicitly enabled. Disabling it restores the usual unlocked start; it does not interrupt the current click-through state.
- The startup preference is saved with general settings and excluded from overlay layout profiles, so applying a layout does not change whether restart restoration is enabled. Settings changes take effect after Save; closing unsaved Settings leaves the saved preference unchanged.
- Added checks for all four enabled/disabled preference and previous-state combinations, persisted settings reload, native DPS click-through after startup, the clickable control panel and profile isolation. Full release validation also covers existing modules and update preservation.
- Includes all Stable 3.0.10 fixes, including transparent idle Boss timing and notification sound recovery. CatScratch light remains unchanged.

## CatScratch v3.0.10

### BOSS TIMING — CLEAN CLICK-THROUGH DISPLAY

- The optional separate Boss timing window now removes its outer background and frame in click-through mode. During a boss fight, only the boss name and readable timing rows remain visible.
- With no active boss, the waiting message is also hidden in click-through mode, leaving the idle overlay fully transparent. Unlocking restores the themed background, frame, title controls and waiting message.
- The hidden controls keep their layout space, so toggling click-through does not shift the boss name or timing rows. Saved position, independent pin and the option to return both displays to the DPS meter remain unchanged.
- Added pixel-level regression coverage verifying that the idle click-through window contains no visible pixels, alongside checks for restoring normal appearance.

### HIDDEN CUBE SOUND — RECOVERY FROM STALLED PLAYBACK

- Each queued notification now uses a fresh Windows audio player. Repeated use of the same custom sound no longer depends on reusing the previous decoder instance.
- Added recovery when Windows does not report that a sound opened or finished. Opening has a five-second limit; playback is released after the validated clip duration plus a short grace period, so a stalled player cannot permanently block later reminders.
- Late callbacks from abandoned players are ignored. Decoder failures advance the queue; Preview still replaces active playback and clears old queued reminders. Shutdown stops playback and its recovery timer.
- Existing sound selection, volume and WAV/MP3 limits are retained. The recovery protects the playback failure path identified during investigation; the exact cause of the original user report has not been confirmed from that user's logs.
- Added deterministic playback checks for repeated reminders, missing open/end callbacks, late callbacks, decoder errors, Preview and disposal. Hidden cube tests also cover new finds after opening earlier cubes and moving between locations.
- This hotfix includes the complete Stable 3.0.9 feature set. Monster pictures or markers are not part of this update. CatScratch light remains unchanged.

## CatScratch v3.0.9

### OPTIONAL SEPARATE BOSS TIMING WINDOW

- **Settings > Combat > Separate boss timing window** moves **Enrage time** and **Estimated defeat** out of the DPS meter into a compact, independently movable window. The option is off by default, preserving the existing DPS layout until you enable it and Save.
- Both displays use the same live data and calculations as the meter. They are removed from the DPS display while detached, avoiding duplicate rows. Boss HP and player damage rows stay in the DPS meter.
- Disable the option or click the timing window's **X** to return both displays to the DPS meter. This closes only the separate timing window, not CatScratch. Save continues to keep Settings open.
- The window has its own position pin and saved screen position. Mode, position and pin participate in overlay profiles. Global show/hide, click-through, display opacity and the selected target screen also apply.
- Hiding the timing controls during click-through reserves their space, so the displayed boss and timing rows do not jump. The window remains a tool overlay without an additional taskbar entry.
- Timing continues updating when the DPS display is hidden. Turning off the DPS boss HP panel does not suppress the detached timing display. Without an active boss, the window shows a waiting message rather than stale boss values.
- **Enrage time** still requires a matching server-provided boss deadline. No timer is invented when that data is unavailable; the label does not imply separately confirmed enrage detection. **Estimated defeat** retains the existing recent boss HP-loss estimate, its insufficient-data state and deadline comparison.
- Test mode includes an isolated timing-window preview when the option is enabled, using the same sample boss as its DPS preview. The existing module positions and real combat data remain separate from the preview.
- Added localized settings, waiting text and window controls for English, German, Russian and French, plus focused checks for detaching, restoring, bindings, persistence, profiles, click-through and preview behavior.
- Includes all Stable 3.0.8 changes. CatScratch light remains unchanged.

## CatScratch v3.0.8

### CHECKLIST — CUSTOM SHORTCUT AND STABLE CLICK-THROUGH PLACEMENT

- Settings > Hotkeys now includes an editable Checklist shortcut. Its default is **Shift+F3**. Select the field and press your preferred combination, then Save. The shortcut toggles the module, just like its control-panel button, while retaining your task text, checkmarks and saved placement.
- The selected shortcut survives restarts and is displayed in the Checklist button tooltip and Help & symbols. Overlay layout profiles do not overwrite it. Existing customized shortcuts remain unchanged.
- Enabling click-through no longer pulls checklist tasks upward when the title controls disappear. The hidden title area retains its space, keeping daily/weekly headings, rows and checkbox hit targets in their original screen positions. Turning click-through off restores the title in the same place.
- Checkboxes remain usable without taking keyboard focus from the game; task text and surrounding transparent space continue to pass clicks through.

### HOTKEY VALIDATION AND REVIEW CLEANUP

- Settings now rejects duplicate or malformed shortcuts before applying changes and directs you to the conflicting field. Different spellings or modifier order for the same actual combination are recognized as a conflict. This prevents one CatScratch action from silently taking another action's shortcut.
- Invalid hotkey strings containing empty segments or unsupported numeric key values are rejected safely. A key combination reserved by Windows or another application may still need to be changed.
- Removed obsolete preview-layout tracking and its unnecessary settings write when ending Test mode. Test mode continues to use the actual user positions, preserving the size of DPS and Settings windows and isolating real tasks and records from sample data.
- Reviewed the affected persistence, profiles, input surfaces, window handling and preview paths, with regression coverage for checklist coordinates in both click-through states, native hotkey dispatch, custom-shortcut reload and profile isolation. Full release validation also covers capture/combat logic, shutdown, performance, sounds, boss records, panel layouts, estimates, placement, color persistence and the updater.
- Includes all Stable 3.0.7 behavior, including the inline Update! title. CatScratch light is unchanged.

## CatScratch v3.0.7

### UPDATE BUTTON — INTEGRATED INTO THE CONTROL PANEL

- The green Update! button now replaces the CatScratch title inside the existing control panel. The detached notification popup above the panel has been removed, so there is no separate notification window to become misplaced.
- The replacement is shown only while a prepared update is available. Without one, the normal CatScratch title remains visible. Removing the prepared update restores the title.
- The installed version stays underneath. The title slot retains its dimensions, so appearing or disappearing updates do not move or resize the panel or rearrange its controls.
- Supports Horizontal, Vertical rail and Grid layouts. The vertical rail uses upright stacked Update! lettering in the same title area; the update indicator stays green in every layout.
- The control remains clickable when overlays use global click-through. Clicking it retains the existing explicit apply-and-restart action; this hotfix does not introduce automatic restarts or change update verification.
- Includes Stable 3.0.6's readable boss remaining-time row and Stable 3.0.5's localized reset settings. DPS layout, settings and user data are retained. CatScratch light remains unchanged.

## CatScratch v3.0.6

### BOSS REST-TIME READABILITY — CLICK-THROUGH HOTFIX

- The estimated remaining boss fight time now has its own compact, rounded dark surface. It remains visible when global click-through removes the surrounding DPS window background, so bright or detailed game scenery no longer sits directly behind the text.
- The estimate uses brighter semibold text and a slightly larger font. The before/after time-limit comparison is also larger and semibold, retaining its existing warning colors.
- The row remains compact and was visually checked in German and English, including the minimum 260-DIP meter width with click-through enabled. The dedicated surface follows the custom interface background/text palette and does not prevent clicks from reaching the game.
- This is a presentation fix: boss HP sampling, estimated-time calculations, deadline comparisons, measurement filters and player-row layout are unchanged. No settings reset is required.
- Includes Stable 3.0.5's localized reset weekdays and English AM/PM correction, together with all Stable 3.0.4 features. CatScratch light remains unchanged.

## CatScratch v3.0.5

### CHECKLIST RESET SETTINGS — LANGUAGE AND CLOCK FORMAT HOTFIX

- Reset weekdays now follow the language selected inside CatScratch, independently of Windows. English shows Sunday–Saturday, German Sonntag–Samstag, Russian воскресенье–суббота, and French dimanche–samedi. An English interface on a German Windows installation no longer displays Mittwoch.
- English reset-time choices use the 12-hour format with AM/PM, including correct midnight (12:00 AM), noon (12:00 PM), and evening values. German, Russian and French keep the 24-hour format.
- Reset countdown tooltips use the same clock convention and localized weekday/date text. UTC remains the schedule reference; the tooltip also shows the next reset in local time. Countdown durations remain elapsed-time durations, without AM/PM.
- This changes displayed labels only. Saved UTC hours, weekdays, task text, completion state and automatic-reset preferences are retained. Save continues to apply changes without closing Settings.
- Includes the complete Stable 3.0.4 feature set. CatScratch light remains unchanged.

## CatScratch v3.0.4

This Stable release brings the full 3.0.4 Beta feature set to the main CatScratch download, together with the final corrections to Test mode, vertical button lettering and DPS overlay placement. It includes every change from Beta 1–13, the checklist heading cleanup prepared for Beta 11, and the additional reviewed fixes. Existing tasks, fights, pet progress and preferences are retained. CatScratch light is a separate download and is not changed by this release.

### MAIN CATSCRATCH PANEL — COMPACT LAYOUT REWORK

- Choose **Horizontal**, **Vertical rail** or **Grid** in Settings. Grid remains available as its own option; selecting Vertical now produces a narrow rail instead of the old grid arrangement.
- Buttons are grouped by function, with fine separators between module controls, analysis tools, measurement/display controls and application actions. The death-recap toggle remains first.
- Horizontal spacing is tighter. The vertical rail uses upright letters stacked under one another for CatScratch and the measurement-mode button, rather than sideways text. Its fight duration and wall clock are hidden to save space.
- The CatScratch letters are closely spaced. The BOSS/ALL/ONLY lettering fits inside its button, including the final S, and is optically centered.
- The Checklist has a dedicated highlighted toggle in the permanent panel. Hiding and reopening it does not change task text or completion.
- The panel remains independent of the DPS display. The DPS position pin still belongs to the meter; the panel has its own position pin. Only the permanent panel's application Exit action closes CatScratch.

### CHECKLIST — DAILY, WEEKLY AND PERSONAL TASKS

Enable **Settings > Checklist > Show checklist**, or use the panel toggle. Choose **1–20 visible task slots** and adjust the window width. The outer window is transparent, with compact tinted task rows for readability.

Type any task on the right and tick its checkbox on the left. Completed text is struck through; unticking immediately restores it. Task text and checkmarks save automatically and survive a restart. Reducing the visible row count retains the hidden rows, including their text and completion, so increasing it restores them.

The starting examples contain two daily and nine weekly tasks: Duty Quests, Field Bosses, Daily Dungeon, Ascension Trial, Battlefield, Sanctuary, PvE Commands, PvP Abyss Commands, Substance Morph, Pass Energy and Daeva Pass EXP. Small **Daily** and **Weekly** headings separate the groups. Extra slots appear under **Other tasks**. Repeated category prefixes have been removed from the example text. The one-time cleanup recognizes the unchanged original examples in supported languages and preserves custom text and completion. These examples are editable reminders, not automatic detection of game progress.

- **Reset countdowns:** Daily and Weekly headings show time until the next configured reset. The tooltip gives the next reset date and local time.
- **Configurable schedule:** defaults are 07:00 UTC daily and Wednesday at the same UTC hour weekly. Change the hour and weekday under Settings > Checklist and verify them for your server. UTC stays fixed when local daylight-saving time changes.
- **Optional clearing:** automatic removal of checkmarks is off by default. If enabled, daily reset clears only slots 1–2, and weekly reset clears only slots 3–11. Missed resets while CatScratch was closed are handled on returning. Task text and Other tasks are never cleared by the schedule. Changing the reset preference starts a fresh baseline instead of immediately erasing completion.
- **Click-through support:** checkbox areas remain clickable while global click-through is on, without taking keyboard focus from the game. Task text and surrounding space still pass clicks through. Unlock to edit the text.
- The checklist supports its own position pin, saved placement, global show/hide and monitor selection. Its X closes only the checklist. Layout profiles restore layout preferences without replacing personal tasks or checkmarks.

### PET TRACKER — FARMING TARGETS, MODES AND SOUND

- Rows now show the **monster to farm**, making it easier to identify the actual target in the game. The tooltip contains only the associated pet name; soul progress remains visible in the row.
- Freshly announced source monsters take priority, with current-region and attacked-source fallbacks. Completed pets remain excluded. Regional recommendations retain the five-source limit; a server announcement is not presented as a measured distance.
- **Settings > Pets > Tracking mode** offers **Nearby & region** and **Pet on hit**. Regional mode remains the default. Pet on hit shows the three most recent incomplete sources personally attacked by the player, without regional recommendations. The selection applies without restarting capture and is retained in layout profiles.
- If collection information is missing, the tracker explains returning briefly to character selection or logging in again so CatScratch can receive fresh collection data. The hint is available in all four interface languages.
- Pet collection/level-up notifications now offer **Chime, Bell or Ping**, an independent **0–100% volume** control and a preview button. Existing installations keep Chime at 50%. Save applies the choice; closing Settings discards unsaved changes. Event audio remains independent.
- Tooltips are reasserted above the overlays when foreground protection restores CatScratch, without activating the application or taking focus from the game.

### HIDDEN CUBES — OPTIONAL PASSIVE ALERT MODULE

Enable **Settings > Hidden cubes > Enable hidden cube alerts (experimental)** to show **HIDDEN CUBE DETECTED** for the cube type confirmed in two approach/open recordings. The module is off by default. It uses a movable, pinnable warning with bold red text and softly pulsing exclamation marks at both ends. Animation stops while the window is hidden.

Opening the detected cube or changing context clears the warning. Announcements expire after 60 seconds without a fresh update, and repeated announcements do not create duplicate windows. Detection means the server announced the supported object; it does not establish an exact distance, direction or continued visual presence. Other cube variants and map placement remain unverified. No game-memory access or screen capture was added.

- **Show test alert** opens a clearly labeled, interactive preview without finding a cube or enabling detection. Repeated preview clicks reuse the same window; closing Settings also closes that test alert.
- Choose an optional custom **MP3 or WAV**, set its own volume and preview it in the same Settings tab. Files are limited to **10 seconds and 10 MB** and must remain available at the selected location.
- A new detection plays one notification. Repeated announcements and visual test alerts do not repeatedly play the sound.
- Placement, pin, global show/hide, click-through and overlay-profile support are retained. The alert X closes its module rather than the application.

### BOSS MEASUREMENT — ONLY BOSS AND DEFEAT ESTIMATES

**ONLY BOSS** is a third option under Settings > Combat. Existing **BOSS** keeps its boss-first measurement with a fallback outside an engaged boss fight; **ALL** keeps measuring all enemy targets. **ONLY BOSS**, labeled **ONLY** on the compact panel, counts damage to the concrete engaged boss actor. It excludes ordinary adds, summoned secondary enemies and other boss actors, and never falls back to trash. Damage from player summons is still attributed to their owner when recognized. Incoming player damage remains available. Changing measurement mode starts a fresh measurement so totals from different filters are not mixed.

A small **estimated remaining boss fight time** appears below the boss HP area. It uses a rolling window of observed boss HP loss, independent of party damage to adds. It can therefore reflect HP loss caused by players whose individual damage packets were not sent to this client. The estimate needs sufficient observations; new targets, healing, stale HP, pauses and completed fights invalidate the current projection rather than displaying a misleading countdown.

When a matching server-provided boss deadline exists, the display compares the estimate against that deadline and indicates whether it is before or after the time limit. **Enrage time remains a server deadline, not a separately confirmed enrage mechanic.** Estimates are projections: changing damage, immunity phases or healing can change the result. Existing class emblems, narrow player rows, class-colored background fills, DPS, percentage share and incoming damage remain in place.

### TEST MODE — EXISTING POSITIONS, ONLY THE REQUESTED WINDOWS

Start Test mode from Settings > General to preview **nine windows**: DPS/personal combat, Pet tracker, Checklist, Hidden cubes, Death recap, Upcoming events, Started events, Statistics and Boss comparison. History, personal-best management, recent teammates, Boss timers management and the event planner no longer open automatically.

- Existing user positions are retained rather than replaced with a stack or an automatic tile layout. Open windows provide their live positions; saved display/event-module positions are used otherwise.
- **DPS and Settings keep their dimensions. Settings also stays in its current position.** Only previews without a known position are given available space, with compact sizing where needed. User-arranged overlap is not silently rearranged.
- Previews are clickable and movable: click-through and movement locks are disabled for the preview. Stopping it restores the selected global click-through state.
- Sample settings, task contents and fights are isolated from real data. No real combat is required, preview edits do not overwrite personal checklist text, and preview windows create no separate taskbar entries.
- Stopping Test mode closes the previews and restores previously visible real windows without permanently enabling disabled modules.

### WINDOW PLACEMENT, MONITORS AND INTERACTION FIXES

- Fixed the DPS overlay moving upward after a restart. The old startup path used the work area above the taskbar, whereas dragging allowed the full screen. Startup now uses the same shared screen-bounds handling. Repeated restoration at the bottom screen edge preserves the saved position and independent pin. If an old build already saved a shifted position, move it back once after updating.
- **Settings > Appearance > Target screen (experimental)** selects the monitor for CatScratch. Save moves the panel and open modules, and future windows use the choice. Windows remain movable afterwards. Automatic preserves saved positions; an unavailable monitor falls back to the primary display. This preference is independent of layout profiles.
- Boss timers is an interactive management window and now remains clickable while overlay click-through is enabled, including after returning to the game. Its X remains usable without unlocking overlays. It keeps a solid background independently of display opacity.
- Settings Save continues to apply preferences without closing the window. The Settings X closes it and discards unsaved edits.

### STABLE UPDATE AND COMPATIBILITY

The installer, portable ZIP and matching source archive are available together. Existing settings and user data are retained. Beta subscribers automatically return to the Stable update channel when this promoted build starts; a later deliberate switch back to Beta remains possible. Downloading/preparing an update still does not restart CatScratch automatically: the restart remains an explicit user action.

This release is for the full CatScratch application. **CatScratch light 1.0.0 remains unchanged**, uses separate downloads/settings and continues to receive work only when explicitly requested.

### VALIDATION

The release runs the full core test suite, integrated window/interaction checks, complete shutdown, performance checks, event audio, boss records, checklist persistence/click-through, the refined preview lifecycle, repeated DPS position restoration and custom-color persistence across a new process. The Windows publication pipeline also verifies the optional game companion and a direct update with retained user data before publishing the installer, portable ZIP and source archive.

## CatScratch v3.0.4-beta.13

- Fixed the DPS overlay moving upward after restarting: startup now uses the same full-screen placement bounds as manual dragging, preserving bottom-edge positions and the independent position pin.
- Added compact Daily and Weekly reset countdowns to the Checklist, with configurable UTC hour (default 07:00) and weekly day (default Wednesday). Automatic clearing is opt-in: daily slots 1–2 and weekly slots 3–11 reset, including after an offline period; task text and Other tasks remain intact.
- Test mode opens fourteen movable display and analysis previews with isolated sample tasks, fights and history. Stopping it closes previews and restores previously visible windows without enabling disabled modules. Preview positions are remembered separately.
- Added ONLY BOSS as a third measurement option. It counts the concrete engaged boss, excluding adds and other boss actors, with no trash fallback. Existing BOSS and ALL modes remain; switching modes starts a fresh measurement.
- Added estimated remaining boss fight time from rolling boss HP loss, independent of damage to adds. Insufficient, stale or healing HP data shows Calculating instead. A matching server deadline permits a before/after time-limit comparison; this does not confirm an enrage mechanic.
- Stable and CatScratch light are unchanged.

## CatScratch v3.0.4-beta.12

- Reworked the control panel into compact Horizontal, Vertical rail and Grid layouts, with matching functional groups and fine separators. The vertical rail uses upright, tightly stacked CatScratch and BOSS/ALL lettering and hides both time displays. The grid remains a separate Settings option.
- Checklist checkboxes now remain clickable during global click-through, without taking keyboard focus. Task text and surrounding space still pass clicks through; unlock to edit tasks.
- Includes the unpublished Beta 11 checklist cleanup: small Daily/Weekly headings, unprefixed example tasks and an Other tasks section for additional slots. Existing custom tasks and completion are preserved.
- The DPS meter layout, Stable release and CatScratch light are unchanged.

## CatScratch v3.0.4-beta.11

- Separated the checklist with small Daily and Weekly headings. The initial two daily and nine weekly example rows no longer repeat category prefixes. Extra free slots appear under Other tasks; headings are hidden when their rows are not visible.
- Existing unchanged example texts in all supported interface languages are cleaned up once, preserving completion state. Custom task text is retained. The compact rows, panel button, automatic saving and 20-slot limit remain available; CatScratch light is unchanged.

## CatScratch v3.0.4-beta.10

- Added a dedicated Checklist button to the permanent control panel. Click it to show or hide the checklist without changing task text or checkmarks; the highlighted icon means enabled.
- Available in horizontal and vertical layouts, including while global click-through is active. The vertical layout uses its remaining fourth-row slot without increasing panel height.
- Added the matching icon and explanation to Settings > Help & symbols. Includes Beta 9's compact transparent checklist, 1–20 slots and 11 editable daily/weekly starting tasks. CatScratch light is unchanged.

## CatScratch v3.0.4-beta.9

- Added an optional compact Checklist module under Settings > Checklist, with 1–20 visible task slots and adjustable width. The surrounding window is transparent; individual task rows have tinted backgrounds for readability.
- Type your own tasks directly in the rows. Checkmarks strike through completed text; unchecking restores it. Text and completion state save automatically and survive restart. Reducing the visible row count keeps hidden tasks.
- Includes 11 editable starting examples from the supplied daily/weekly overview: two daily and nine weekly tasks. Counts are example text, not automatic game tracking; checkmarks do not reset on a schedule.
- Supports an independent position pin, saved placement, monitor selection, global show/hide and click-through. Turn click-through off to edit. Profiles restore checklist layout without overwriting personal task text or completion. The checklist closes with CatScratch; CatScratch light is unchanged.

## CatScratch v3.0.4-beta.8

- Fixed Boss timers becoming unclickable when global click-through was enabled. This management window now stays interactive like fight history, including after switching back to the game; its X remains available without unlocking the overlays.
- Boss timers keeps a solid background independently of overlay opacity, while remaining a separate tool window. Display overlays still respect the global click-through setting.
- Includes Beta 7's custom MP3/WAV Hidden cube notification, volume and preview, plus the explicitly experimental target-screen selector. Added checks for inactive-window close-button hit testing and repeated lock/unlock transitions.

## CatScratch v3.0.4-beta.7

- Added a custom MP3 or WAV notification for Hidden cube alerts, with its own volume slider and audio preview under Settings > Hidden cubes. Audio is optional; a valid file selection enables its draft, and Save applies it.
- A new cube detection plays the selected sound once. Repeated announcements and visual test alerts do not repeat the notification. Files are limited to 10 seconds and 10 MB and must remain available at the selected location.
- Added Settings > Appearance > Target screen (experimental). Save moves the control panel and open overlay modules to the chosen monitor; new modules and Settings use it too. Windows remain freely movable afterwards.
- The screen preference is saved for the next start. Automatic preserves saved window positions; an unavailable selected monitor falls back to the primary screen. Monitor selection stays independent of overlay profiles.

## CatScratch v3.0.4-beta.6

- Made HIDDEN CUBE DETECTED more prominent with bold red text and softly pulsing exclamation marks on both sides. Animations stop while hidden.
- Added Settings > Hidden cubes > Show test alert, so the window can be positioned and pinned without finding a cube. The labeled preview stays interactive and does not enable detection or create a fake cube event.
- Repeated previews reuse one window; closing Settings also closes the test alert. Placement and pin remain saved.

## CatScratch v3.0.4-beta.5

- Added an optional, passive Hidden cubes module with a movable, pinnable HIDDEN CUBE DETECTED alert. Enable it under Settings > Hidden cubes; it is off by default.
- The confirmed cube type was verified against two approach/open recordings. Alerts clear after opening, context changes or 60 seconds without a fresh announcement; duplicate announcements do not create duplicate windows.
- Includes click-through, saved placement and overlay-profile support. Other cube variants, map placement, direction and distance are not yet verified; no screen capture or game-memory access was added.

## CatScratch v3.0.4-beta.4

- Added Settings > Pets > Tracking mode: choose Nearby & region or Pet on hit.
- Pet on hit shows only your three most recent personally attacked, incomplete pet sources. Regional mode remains the default; completed pets stay hidden in both modes.
- The selected mode is saved with Settings and overlay profiles, and switches without restarting capture.

## CatScratch v3.0.4-beta.3

- Keeps open tooltips above overlays when the foreground protection restores CatScratch, without taking keyboard focus.
- Pet tracker tooltips now contain only the pet name; the farming monster and soul progress remain in the row.

## CatScratch v3.0.4-beta.2

- Pet tracker rows now show the farming monster; the pet name remains in the tooltip. Freshly seen sources take priority, with current-region and attacked-source fallbacks.
- Missing collection data now explains returning to character selection or logging in again, in all four languages.
- Keeps the five-pet limit, completed-pet filtering, soul progress and independent sound controls.

## CatScratch v3.0.4-beta.1

- Added Chime, Bell and Ping selection, independent 0–100% volume and a preview button in Settings > Pets.
- Pet notification sound preferences are applied and retained with Save; unsaved edits are discarded when Settings is closed.
- Existing installations keep Chime at 50% volume. Event sounds remain independent.

## CatScratch v3.0.3

This Stable update makes the central Settings navigation easier to scan. The existing CatScratch styling and DPS meter layout are preserved, and all settings retain their current values and behavior.

### SETTINGS — ORGANIZED BY TOPIC

The left sidebar now follows a fixed topic-based order rather than mixing general options, individual modules and support tools:

- **General & appearance:** General, Appearance, Overlay profiles.
- **Game modules:** Combat, Events, Pets.
- **Controls & data:** Hotkeys, History, Capture & data.
- **Updates & help:** Updates, Feedback, Help & symbols.

Each group has a clear heading in the current accent color and a horizontal dividing line. Compact spacing keeps all twelve tabs visible at the normal window height; the sidebar remains scrollable in smaller windows. Headings are translated for English, German, French and Russian, while the topic order stays consistent between languages.

### EXISTING SETTINGS BEHAVIOR PRESERVED

- Appearance remains the initially selected page, so the familiar display controls stay directly accessible.
- Events shortcuts and invalid-color focus use stable section identifiers, keeping navigation correct after the reorder.
- Save continues to apply and persist changes without closing Settings. The X closes the window and discards later unsaved edits.
- Profiles, module visibility, capture, hotkeys, updates and feedback retain their existing functions. No preference reset or migration is required.
- The combat display, class colors, statistics, control panel and other overlay modules retain their existing layout.

### VALIDATION

- The application built without warnings or errors, and all 1,127 integrated interaction checks passed after the sidebar changes.
- The grouped sidebar was rendered and visually checked at the standard window size.
- The release pipeline additionally runs core tests, window/shutdown checks, color persistence, optional game companion checks and the direct update test before publishing the downloads.
## CatScratch v3.0.2

This Stable maintenance update keeps the complete 3.0.1 feature set and existing CatScratch layout. It corrects three edge cases found during a broader review of updates, capture restarts and module behavior. Your settings, saved fights, boss personal bests, recent teammates and pet collection progress are retained.

### UPDATE CHECKS — CLEAN SHUTDOWN

- Closing CatScratch now cancels a running release check through the updater's own lifetime token. An unfinished background request cannot subsequently populate release information or notify closed UI subscribers.
- A stopped updater cannot begin another release check or prepare another update. Repeated cleanup is safe.
- Startup checking and the ten-minute automatic interval remain unchanged. Prepared updates still require your explicit restart click; this update does not introduce automatic restarts.

### MORE ROBUST RELEASE DATA HANDLING

- Unexpected non-object entries in release responses are ignored safely.
- Download files with missing, invalid, fractional, zero, negative or overflowing sizes are excluded. They cannot become downloadable update packages or cause an uncaught numeric-conversion error.
- An invalid release-list structure is reported through the existing handled JSON-error path. A bad file entry does not prevent otherwise valid release notes from being available.
- Existing package size and SHA-256 verification remains in place.

### PET TRACKER — CAPTURE RESTART CORRECTION

- Restarting packet capture now clears pending soul-drop correlations along with old location and monster context.
- If the game reuses an actor identifier after the restart, an old soul drop can no longer be incorrectly assigned to a different monster as a learned farming source.
- Confirmed pet collection progress is preserved. New valid soul drops can still teach their actual farming source normally. The five-recommendation limit, completed-pet filtering and passive-only operation are unchanged.

### VERIFICATION AND COMPATIBILITY

- All 384 core tests passed, including ten new regression cases for these fixes.
- The Release application build completed without warnings or errors.
- All 1,127 integrated interaction checks passed, covering Settings, module controls, click-through, pins, profiles, colors and pet presentation. Additional update/channel/preparation, boss-record, event-audio and full-shutdown checks passed.
- Custom colors and Mario Party settings were verified across separate program starts. The optional game companion passed isolated tests for windowless startup, exact executable matching, one launch per game session and disabling the watcher.
- The existing compact meter layout and all 3.0.1 modules remain available. Stable subscribers receive this maintenance version normally; existing Beta-to-Stable transition behavior is retained.
- Automated checks cannot cover every live-game packet or a prolonged in-game session. Dependency vulnerability metadata was unavailable during the local review, so this release does not claim a completed dependency security audit.
## CatScratch v3.0.1

This Stable release brings together the complete 3.0.1 Beta series through beta.25. It replaces Stable 3.0.0 and includes the new independent control panel, the reworked Pet tracker, interface customization, the 24-hour event planner, effective personal healing and the fixes tested during this Beta cycle. Existing local preferences and records are retained.

Jump to: [Main CatScratch Panel](#main-catscratch-panel--your-new-control-center) · [Pet Tracker Rework](#pet-tracker-rework--five-focused-recommendations) · [Colors and transparency](#interface-colors-transparency-and-mario-party) · [Events](#24-hour-event-timeline-and-favorites) · [Healing](#effective-self-healing-and-healing-others) · [Updates](#updates-restarts-and-moving-from-beta-to-stable)

### MAIN CATSCRATCH PANEL — YOUR NEW CONTROL CENTER

The CatScratch control panel is now a permanent, separate part of the overlay. It keeps the controls available even when you hide the DPS display or enable click-through. The combat display and the controls can be placed independently.

#### All general controls in one place

- The installed version appears underneath the CatScratch name in the control panel. Stable installations display **3.0.1**, without a beta suffix.
- General header and footer actions now live in the panel: module toggles, personal combat, Pets, boss personal bests, recent players, statistics, fight details, exports, patch notes, saving, update checks, timers, reset, damage mode and background opacity.
- The Settings gear remains directly accessible in the panel. You no longer have to restore the DPS display or remember a shortcut just to open Settings.
- The panel's **X** is the only in-app action that exits CatScratch. The visible Exit control was removed from the DPS display and the tray Exit entry was removed. Closing an individual auxiliary module only closes that module.
- The panel stays clickable during global click-through so you retain access to Settings, visibility controls and Exit.

#### Compact horizontal and vertical layouts

- Choose the orientation under **Settings > Appearance > Control panel**. The horizontal layout keeps its compact toolbar arrangement.
- The vertical layout now uses four aligned symbol columns, tighter rows and subtle vertical separators. Its panel width is reduced to **132 logical pixels**, replacing the uneven wrapping and excessive gaps from early Beta builds.
- Damage mode and background opacity have their own bottom row instead of being scattered among the module icons. Icons retain their actions and tooltips when you change orientation.
- The panel supports free placement and nine screen anchors: corners, edge centers and screen center. It can reach the full screen edge, including positions around the taskbar area.
- Dragging changes the saved positioning mode to free placement. Choosing an anchor or orientation in Settings is applied when you press Save.
- Panel placement, orientation, alignment and pin state are saved and included in overlay profiles. The DPS display retains its own dimensions and location.

#### Independent DPS display and position pins

- **Settings > Appearance > Modules > Show DPS meter** can disable only the combat display while capture and the other enabled modules continue running.
- Restore the combat display with its panel button, tray control or configured shortcut; the default DPS-display shortcut remains **Shift+F5**.
- Automatic visibility and Show/hide all respect a deliberately disabled DPS display rather than turning it back on unexpectedly.
- The DPS meter's position pin is back at the upper right of the actual DPS display. The panel's pin controls only the panel. Both pins remain independent and preserve click-through behavior.
- The existing compact combat rows remain: class emblems, class-colored background fills, DPS, contribution percentage, CP/GS when received and red incoming damage. General controls were moved without replacing those combat statistics.

### PET TRACKER REWORK — FIVE FOCUSED RECOMMENDATIONS

The Pet tracker can now recommend useful pets without requiring you to attack a monster first. It uses the confirmed current region and freshly received source-monster announcements, while keeping the overlay compact and preserving unknown collection data.

#### Sight and region recommendations

- The tracker displays **at most five recommendations**. Freshly recognized pet-source monsters in sight take priority; the remaining entries are suggestions for the current supported region.
- A **⌖** marker identifies a pet whose source monster was freshly observed. A regional entry means a recorded farming source exists in that region; it does not mean that monster is currently beside your character.
- Region-based recommendations support **Verteron, Altgard and Chaotic Lower Reshanta**. The offline location data contains **156 matched pets**, complementing the existing confirmed Global monster-source mappings for **165 pets**.
- The source data is bundled with CatScratch and does not need a website request whenever the overlay refreshes. The map records are attributed to AION 2 Pets / Wikily; region identifiers were cross-checked against the Global map index.
- These are sight- and region-based recommendations, **not the five closest pets by measured distance**. Reliable continuous own-player coordinates and a verified map-pixel conversion are not available, so no exact distances, navigation arrows or player radar are displayed.
- CatScratch remains passive. This release does not add game-memory access, game input automation or outgoing game packets for positioning.

#### Completed pets stay out of the way

- Pets confirmed at **level 3** are excluded from every Pet tracker view, including the recently attacked-source fallback.
- When the final soul loot confirms completion, the pet disappears immediately. You do not have to restart CatScratch or change areas to remove it.
- If no supported location or fresh nearby source is known, the tracker can fall back to the three most recently attacked incomplete pets. Completed pets no longer occupy one of those slots.
- The Global progression remains **5 souls to unlock, then 25 and 75** for the following levels. Rows show the current soul count and progress toward the next level; the tooltip explains how many souls are still needed.

#### Honest progress and current location context

- Recommendations require a confirmed own collection baseline. Missing collection information is not treated as an empty collection or as proof that every pet is missing.
- Progress remains saved per character. A **~** prefix identifies last-known cached counts until fresh collection data arrives. A new baseline replaces those cached values rather than adding them twice.
- Map changes, character changes and capture resets clear old location context. The previous region and its nearby monsters are not silently reused after a disconnect or teleport.
- Fresh sightings are time-limited, dead recognized NPCs leave the nearby list, and recently attacked sources retain their separate fallback role.
- The open module refreshes its location recommendations regularly. Unchanged rows are not rebound unnecessarily, which preserves scrolling while you inspect the list.
- Pet width can be adjusted under **Settings > Pets**, with a wider **150–1600 logical-pixel** range. A small bottom-right grip also adjusts width directly; the selected width is saved between starts. The grip is hidden during click-through.

#### INTERFACE COLORS, TRANSPARENCY AND MARIO PARTY

- **Settings > Appearance > Interface colors** lets you customize the accent, general background and text colors. Semantic class, healing, damage, event-type and favorite colors retain their distinct meanings.
- The color picker now uses a saturation/brightness field and a rainbow hue strip. You can also enter RGB hex values, use presets and preview your selection before saving.
- Keyboard support includes arrow-key adjustments, Space to switch between the color field and hue control, and Shift for larger steps.
- Valid low-contrast choices are allowed: the contrast warning is advisory and no longer blocks Save without explanation. Invalid hex input is brought into view for correction.
- Saved palettes apply across open windows and persist after restarting. Fixes prevent cached meter surfaces from restoring an old color after click-through changes or Restore default colors.
- Background opacity now reaches **0%** for the DPS meter and control panel. The extra dark meter surface follows the setting too, rather than remaining opaque behind the adjustable layer. Text and icons do not receive an additional background-only fade.
- Display opacity and background opacity remain separate controls. Panel transparency, saved palettes and position anchors continue to work together.
- The small **?** next to Restore default colors enables the optional **Mario Party** mode. After Save, a seamless rainbow wallpaper scrolls across general window backgrounds on a **12-second loop**, with smooth transitions instead of abrupt color jumps.
- Restore default colors and Save stops Mario Party and returns to the standard palette. Semantic combat colors, class colors, event colors and gold favorite frames are preserved.

#### 24-HOUR EVENT TIMELINE AND FAVORITES

- The clock in Upcoming events opens a compact **24-hour horizontal timeline**, with a separate track for each event type and times for the selected region.
- The planner includes scheduled event types and known next world-boss spawns. Its height was tightened to avoid a large empty lower area.
- Star an event type to save it as a favorite. Its timeline occurrences and matching overlay cards receive a thin, softly glowing **gold frame**.
- Favorites are independent of which events you show in the overlay and which reminder sounds you enable; favoriting does not silently change those preferences.
- Event categories are easier to distinguish: **Event is turquoise, PvP lavender and Boss orange**.
- The planner refreshes every minute while open and refreshes when reopened. It uses the bundled community schedule and selected region; it is not a live feed of official schedule changes.
- Started events retain their separate module: normal events remain Open for 3 minutes, Spacetime Rift for 10 minutes, and bosses show Spawned for 2 minutes. Started entries leave Upcoming rather than appearing in both places.

#### EFFECTIVE SELF HEALING AND HEALING OTHERS

- Both values in Personal combat now show **HP-confirmed effective healing**, excluding overhealing. A reported heal amount alone is not counted as proof of restored health.
- Supported direct group heals previously carried in damage-shaped packets are now recognized as healing events. Healing-over-time ticks use the same confirmation rules; damage totals are not inflated by those heals.
- Only a matching, sufficiently fresh and unambiguous HP increase is credited. Overlapping healers, concurrent damage and missing or inconsistent HP evidence are left unconfirmed instead of being guessed.
- A **\*** marks an incomplete confirmed subtotal. A **dash** means healing was reported but no amount could be confirmed, or the necessary effective-healing field is unavailable. Tooltips explain the distinction.
- Existing raw healing and self-healing fields in historical statistics retain their original meaning. Personal combat uses separate effective values; older saved records are not retroactively converted into verified healing.

#### BOSS HP AND ENRAGE TIME

- The boss HP display uses a refined rounded fill with separate HP and percentage columns, improving readability in narrow meter layouts.
- A small **Enrage time** bar appears underneath when the server supplies a matching deadline for the current boss. It updates from that deadline and clears when it expires or the boss/timer context resets.
- This label represents the received boss time limit. It is **not a separately verified enrage-mechanic prediction**. An instance-wide timer is never substituted for a missing boss deadline.
- When the small boss countdown is displayed, the same boss countdown is not duplicated in the header; a separate valid instance timer may still appear there.

#### SETTINGS, HELP AND WINDOW RELIABILITY

- Pressing **Save** now persists and applies changes while keeping the same Settings window open. Use its **X** to close it; later unsaved draft edits are discarded. The closing Cancel button was removed.
- Saving a panel layout also clears its pending-change marker, so an unrelated later Save does not reapply an old layout choice after you have moved the panel.
- **Settings > Help & symbols** provides a read-only icon guide with functions, state explanations and your saved shortcuts. The guide is available in English and German and does not execute the example actions.
- Visible overlay windows now recover their native always-on-top ordering if a game or display change places them behind other windows. Recovery uses no activation, movement or resizing.
- Hidden modules remain hidden. While you use CatScratch Settings or another interactive CatScratch dialog, the recovery routine does not force display modules over it.
- Current screenshots and the GitHub feature guide were updated during this Beta cycle, including the revised Pet tracker. Captures and personal gameplay data are not included in release downloads.

#### UPDATES, RESTARTS AND MOVING FROM BETA TO STABLE

- Automatic update checks still run shortly after startup and **every ten minutes** when enabled. Available packages are downloaded, verified and extracted in the background while the current version keeps running.
- A prepared update appears as a small, **green Update! button in a separate popup above the control panel**. The text fits its compact button and is no longer clipped.
- The notice follows the panel and hides with it. Showing or removing it does not expand, move or re-anchor the panel. Clicking it uses the existing prepared-update restart action.
- CatScratch does not restart without your update click. After the new version successfully starts, the old backup, staged payload and downloaded archive are cleaned up; failed updates retain recovery files.
- Direct updates now refresh icons for existing desktop, Start menu and pinned-taskbar shortcuts that target the updated installation. The installer also requests a Windows icon-association refresh. A shortcut targeting a different old copy is not redirected or deleted.
- On the first successful start of **3.0.1 Stable**, Beta subscribers automatically return to the **Stable** update channel. The displayed version becomes **3.0.1**, with no beta suffix.
- If you deliberately select Beta again later, that selection survives normal restarts until a future Stable promotion. The public Beta branch and previous prereleases remain available; this release does not add a password gate.
- Local settings, saved fights, boss personal bests, recent players, pet progress and debug captures remain preserved through the update.

## CatScratch v3.0.1-beta.25

- Limited the Pet tracker to five recommendations, with fresh sources in sight taking priority over regional suggestions. No exact-distance ranking is implied.
- Completed level-3 pets are now hidden in every Pet tracker view, including recently attacked sources, and disappear immediately when final soul loot confirms completion.
- Made the separate Update! notice green and fixed its clipped text while preserving the control panel position and size.

## CatScratch v3.0.1-beta.24

- Pet tracker now recommends incomplete pets in supported current regions without requiring an attack. Freshly seen source monsters take priority and mark their pets with a location symbol.
- Added offline farming records for 156 matched pets across Verteron, Altgard and Chaotic Lower Reshanta, with source attribution. Region records are not guaranteed nearby spawns; exact distances remain unverified and are not shown.
- Preserved unknown and cached collection states, kept the attacked-target fallback, and cleared old location context on map, character or capture changes. Refreshing unchanged recommendations preserves scrolling.

## CatScratch v3.0.1-beta.23

- Ready updates now appear as a small, clickable Update! notice above the control panel instead of expanding the panel.
- The notice follows the panel and hides with it. Showing or hiding an update leaves the panel position and size unchanged.

## CatScratch v3.0.1-beta.22

- Hotfix: recover visible overlays that lose their always-on-top position after game activation or display changes.
- Recovery keeps position, size and click-through unchanged, does not activate the overlays, and leaves hidden modules hidden. CatScratch dialogs keep priority while you use them.

## CatScratch v3.0.1-beta.21

- Saving Settings now applies changes without closing the window. Use the X to close; unsaved changes are discarded.
- Restored Pet tracker width adjustment with a corner resize grip and a wider range in Settings > Pets. Width is saved between starts.
- Updates now refresh icons for existing desktop, Start menu and taskbar shortcuts targeting the updated installation. Shortcuts to other copies are left unchanged.

## CatScratch v3.0.1-beta.20

- Made the vertical control panel narrower with tighter, aligned icon rows and subtle vertical separators between its four columns. Horizontal layout is unchanged.
- Background opacity now reaches 0% for the DPS meter and control panel while keeping text and icons readable.
- Fixed the extra dark meter backdrop that remained visible when lowering background opacity.
- Preserved transparent backgrounds across settings, restarts and click-through changes.

## CatScratch v3.0.1-beta.16

- Reworked the vertical control panel with a clear branding/header area and a uniform four-by-four action grid.
- Grouped damage mode and background opacity into their own bottom row; removed uneven wrapping and scattered columns.
- Kept the horizontal layout and all original button actions. Switching orientations preserves controls, tooltips, transparency and screen alignment.
- Returned the DPS position pin to the DPS display; the panel keeps only its own independent pin.
- Refined boss HP with rounded fills and separate HP/percentage columns. Added a small Enrage time bar underneath only when a matching server deadline is available; this is not a separately confirmed enrage timer.
- Self healing now follows Healing others: only HP-confirmed effective healing is displayed, excluding overhealing. Incomplete confirmed totals are marked with *; unconfirmed amounts are not counted.

## CatScratch v3.0.1-beta.13

- Added Settings > Help & symbols: a read-only English/German icon guide with functions, state explanations and your configured shortcuts.
- Made the control panel more compact and added horizontal / vertical layouts, nine screen anchors and free placement in Appearance > Control panel.
- Fixed placement at the full screen edge and applied display/background opacity to the panel while keeping its controls clickable.
- Panel layout and alignment persist across restarts and overlay profiles. Dragging switches back to free placement; Save/Cancel apply to the new options.

## CatScratch v3.0.1-beta.12

- The compact CatScratch control panel is now always available, independently of the DPS combat display.
- Moved the installed version and general header/footer controls into the panel, retaining the existing combat rows and statistics.
- The panel X is the only in-app action that exits CatScratch. Removed the DPS display's visible Exit controls and the tray Exit entry.
- Panel and meter have independent placement and position pins, retained in overlay profiles. Panel controls remain clickable during global click-through.

## CatScratch v3.0.1-beta.11

- Hiding the DPS meter now leaves a small CatScratch control bar with direct Settings access and a button to restore the meter.
- The movable, pinnable bar stays clickable even with global click-through enabled and adds no extra taskbar entry.
- Capture, other modules, saved meter dimensions and Show/hide all continue working independently.

## CatScratch v3.0.1-beta.10

- Added Settings > Appearance > Modules > Show DPS meter. Disable the display while capture and other modules keep running.
- Restore the meter through its tray icon or configured shortcut (Shift+F5 by default). Automatic visibility never overrides a disabled display.
- The choice persists across restarts and is included in overlay profiles. Save applies it; Cancel discards changes.
- Show/hide all respects the disabled meter and continues restoring the other modules.

## CatScratch v3.0.1-beta.9

- Healing others now shows HP-confirmed effective healing and excludes overhealing.
- Direct group heals previously ignored in damage packets are now recognized correctly; damage totals stay unaffected.
- Incomplete verified totals have an asterisk and an explanatory tooltip. Unconfirmed healing is excluded; a dash indicates that no healing could be confirmed.
- Existing self-healing and reported-healing history fields are preserved.

## CatScratch v3.0.1-beta.8

- Mario Party now scrolls a continuous rainbow wallpaper across the general window backgrounds rather than cycling individual flat colors.
- Doubled the speed to a seamless 12-second loop. Class, combat and event colors, control colors and click-through transparency are preserved.
- Restore default colors and Save still stops the effect and restores the regular palette.

## CatScratch v3.0.1-beta.7

- Added a hidden Mario Party! gimmick: the ? next to Restore default colors enables smoothly blended rainbow backgrounds after Save, with a seamless 24-second loop.
- The mode is saved across restarts. Restore default colors and Save stops it; Cancel discards a draft choice.
- Class, combat, event and favorite colors remain unchanged. Includes the color saving and reset fixes from beta.5.

## CatScratch v3.0.1-beta.5

- Fixed valid color choices being blocked by the contrast check. Low contrast is now advice; your chosen colors are applied and saved.
- Fixed the meter header keeping an old background after color changes, resetting defaults or toggling click-through.
- Saved palettes now remain exactly as chosen after restart. Invalid hex input is brought into view instead of leaving Save unexplained.

## CatScratch v3.0.1-beta.4

- Replaced the RGB sliders with a saturation/brightness color field and a rainbow hue strip.
- Drag to choose a color, or use the keyboard: arrows adjust the selection, Space switches between the field and hue, and Shift makes larger steps.
- Hex input, presets, preview, default reset and Save/Cancel behavior are retained.

## CatScratch v3.0.1-beta.3

- Customize accent, background and text colors under Settings > Appearance > Interface colors.
- Use the built-in RGB picker, color presets or hex values with a local preview and a default-color reset.
- Save applies the palette across open windows and preserves it after restart. Cancel discards the draft.
- Readability checks and automatic button-label contrast help keep custom palettes usable. Class, combat, event and favorite colors keep their meaning.

## CatScratch v3.0.1-beta.2

- The Upcoming events clock opens a horizontal 24-hour timeline with a separate track per event and regional start times.
- Favorite event types with the star. Favorites are saved and receive a subtle glowing gold frame in the timeline and event overlays.
- Event, PvP and boss types now have distinct turquoise, lavender and orange accents.
- Last played with uses the shared crisp vector cat paw.
- Started events remains a separate module, available through Settings > Events.

## CatScratch v3.0.0

### Complete Beta feature set promoted to Stable
This release brings all changes tested in the 2.5.6 and 2.6.0 Beta series to Stable, together with separate Updates and Feedback settings tabs. It replaces Stable 2.5.5. Existing local settings and records are retained.

### One organized Settings window
- The DPS meter contains the single Settings gear. Event and pet overlays use their own dedicated tabs in that central window rather than opening separate preference menus.
- **Updates** is now an independent tab for the update channel, background checks, installed version, manual checks and patch notes. **Feedback** has its own tab containing the bug-report / feature-request controls. Both sections have been removed from General.
- **General** keeps test mode, language and the optional AION 2 companion. **Events** groups visibility, schedule region, event selection, module widths, opacity, boss reminders and sound preferences. **Pets** contains tracker visibility, width and collection/level-up sounds.
- Related options use compact, expandable sections. The category list can scroll in smaller windows, form rows wrap and labels remain light against neutral gray surfaces.
- Event, pet and death-recap options now follow **Save / Cancel**. Closing without saving discards their edits. Explicit actions such as sound previews, debug exports and managing saved layout profiles remain immediate.

### Compact meter and consistent appearance
- Retained the familiar DPS meter layout while narrowing player rows. Each row keeps its class emblem, class-colored background progress fill, DPS, damage percentage and red incoming damage. Removed the thin progress strip and total outgoing damage from these rows; detailed combat statistics remain available in the fight window.
- CP and GS use the available second-line width, improving readability in narrow meters. Class, healing and damage colors keep their meanings.
- Neutral dark gray backgrounds and restrained lavender accents are used throughout the interface. The new outlined purple cat paw has four toes and a pad; window headers use a scalable vector, while the executable and tray include native icon sizes.
- The installed version appears beneath the CatScratch title. Footer controls have matching proportions, and the death-recap tombstone toggle is first on the left.
- The Rotation tab is explicitly marked **WORK IN PROGRESS**.

### Position pins and optional overlay profiles
- Display modules have a compact position pin at the upper right. Pinning prevents accidental dragging while leaving controls, resizing and click-through behavior independent. Pin states persist across restarts.
- **Settings > Overlay profiles** can save, apply, update and delete named layouts. Profiles include module positions, sizes, visibility, appearance and position pins.
- Layout profiles do not change capture configuration, update subscriptions, hotkeys or game-start preferences. Applying a profile acts immediately, closes Settings and discards other unsaved edits.

### Automatic boss fight comparison
- Open the statistics for a finished boss fight and use the comparison button. CatScratch selects the latest earlier matching recorded run automatically; both cards clearly show the boss name, date and duration.
- Compares duration, DPS, outgoing damage, healing, incoming damage and deaths. Matching requires the same boss, map, character, outcome and damage-counting mode. Unknown death counts remain unknown, and possibly partial recordings are flagged.
- Healing and incoming-damage differences are descriptive rather than automatic performance judgments. The comparison uses existing session and saved fights; automatic history saving is not enabled implicitly. Save fights if you want comparisons after restarting.
- Instance variants sharing a map ID cannot be distinguished automatically. Opening old fights or comparisons leaves the meter following **Live**; reopening the same fight reuses its statistics window.

### Compact death recap
- Shows only the five latest received hits, newest first, with red damage values and a total for those displayed hits. Removed duplicate attacker totals, the routine footer, scrolling and the resize grip.
- Defaults to the upper center of the meter's screen, above standard resurrection controls. Its dark panel remains readable in click-through mode. Dragging and automatic hiding after confirmed resurrection remain available.
- The tombstone button and configurable death-recap hotkey remain available through the meter and central Settings.

### Group display and debug convenience
- Fixed a five-player roster case where a member could disappear when the server encoded in their database ID differed from explicit server fields. Validated roster records now preserve the member's identity, class, CP and GS consistently.
- Added **Open captures folder** beside **Save recent debug**, making locally saved parser recordings easier to find. Recordings remain local unless you choose to share them.

### Optional automatic start and close with AION 2
- Enable the combined companion option in **Settings > General**, choose the actual game executable and save. It is off by default. Detection follows the selected executable, including launches through Steam or Purple.
- A per-user watcher starts at Windows sign-in and launches CatScratch once per detected game session. CatScratch closes after the game has been absent for ten seconds; manually quitting the meter does not cause repeated launches during that same session.
- A windowless helper starts the local PowerShell watcher without allocating a console, preventing the brief PowerShell flash. Existing enabled installations migrate to this quieter task. Disabling the option removes its registration; uninstall removes the task too.

### Updates, persistence and verification
- Users moving from the Beta series to **3.0.0** automatically return to the **Stable** update channel when the promoted version starts. The displayed installed version is **3.0.0**, without a Beta suffix. A later manual Beta selection remains selected until a subsequent promotion.
- Background checks continue shortly after startup and every ten minutes. A selected-channel update is downloaded, verified and prepared before restart is offered. Restart still requires the user's action.
- Successful updated startup removes the old update backup, prepared package and download. Failed updates retain recovery files; settings, saved fights, boss bests, recent players and captures are preserved.
- Settings, event preferences and overlay profiles use atomic writes with a last readable backup. Added recovery, canceled-edit, narrow-row and text-contrast checks alongside the existing combat and window tests.
- Release verification covers the core test suite, overlay interaction and shutdown, event audio, boss records, Beta-to-Stable transition, the quiet game companion, and direct updates with preserved user data.

## CatScratch v2.6.0-beta.2

### Visual polish and safer preferences
- Neutral dark gray surfaces, restrained purple accents and a crisp outlined cat paw across the app, including Windows and tray icons.
- Slimmer DPS meter rows with class-colored background fills, class emblems, DPS, percentage share and red incoming damage. Removed the thin progress strip and total outgoing damage from player rows.
- Tidier, expandable central Settings and a clearer boss comparison. Event, pet and death-recap options now follow Save/Cancel; preference files keep recoverable backups. Rotation is marked **WORK IN PROGRESS**.
- Improved narrow-window layouts, text contrast and icon scaling, with additional checks for settings recovery, canceled edits and compact player rows.

## CatScratch v2.6.0-beta.1

### Central settings and position pins
- One Settings menu from the DPS meter gear: event selection, appearance, reminders and sounds now share the Events tab; pet display and sounds have their own Pets tab. Removed module settings gears.
- Added matching position-pin buttons to display modules. Pinning prevents dragging while controls and click-through retain their existing behavior; pin states persist and are included in optional overlay profiles.
- Moved the version beneath the CatScratch title, freed the footer for buttons and placed the death-recap toggle first. Carries forward all previous 2.5.6 Beta features, including the compact recap, profiles and automatic boss comparison.

## CatScratch v2.5.6-beta.11

### Optional layouts and boss fight comparison
- Added **Overlay profiles** as a separate Settings tab: save, apply, update and delete named layouts. Profiles restore module placement, visibility, scale and appearance without changing capture, updates, hotkeys or autostart preferences.
- Added **Boss fight comparison** to finished boss statistics. Compare your duration, DPS, damage, healing, damage taken and deaths automatically with the last earlier matching session or saved fight. Both run cards show the boss name clearly; the meter stays Live. Compact dark rows highlight current values and use turquoise/red differences, accounting for lower-is-better statistics.
- Comparisons match boss, map, character, outcome and damage mode, preserve unknown death counts and flag possibly partial recordings. Instance variants sharing the same map ID cannot be distinguished. Save fights to retain comparisons across restarts.

## CatScratch v2.5.6-beta.7

### Compact death recap
- Shows only the five most recent received hits, newest first, with red damage values and a matching five-hit total.
- Cleaner CatScratch layout without duplicate attacker totals, scrollbar or resize grip; the dark surface stays readable in click-through mode.
- Opens centered in the upper half of the meter screen, above the standard resurrection controls. Dragging and automatic hiding after resurrection remain available.

## CatScratch v2.5.6-beta.6

### Group recognition, live statistics and debug convenience
- Fixed five-player rosters losing a member when the server encoded in their database ID differs from the explicit server fields. CP/GS, member updates and exact dungeon identity matching now use the validated record correctly.
- Selecting an earlier session or saved fight opens its statistics immediately in a separate window; the DPS meter always stays Live. Repeated clicks reuse the same statistics window.
- Added **Open captures folder** next to Save recent debug, using the existing compact CatScratch style in all four languages.
- The optional AION 2 watcher now starts through a windowless helper to prevent the brief PowerShell console flash. Existing enabled installations migrate on first updated startup; the watcher remains optional and avoids duplicate instances.
- Expanded the GitHub guide with the purpose of autostart, why PowerShell is used, local checks, permissions and how to disable it.

## CatScratch v2.5.6-beta.4

### Optional AION 2 companion
- Enable **Start and close CatScratch with AION 2** in Settings > General, then choose the actual game EXE. The option is off by default and also works when the selected game is launched through Steam or Purple.
- A small per-user background watcher starts at Windows sign-in and opens CatScratch when the selected game starts. CatScratch closes after the game has been absent for ten seconds; manually quitting the meter does not reopen it during the same game session.
- Turning the option off removes the watcher registration. The watcher does not lock update binaries; uninstall also removes its scheduled task. Settings use the existing compact CatScratch design in all four interface languages.

## CatScratch v2.5.5

### Hotfix: one CatScratch taskbar entry
- Upcoming events, Started events and the other display modules are now registered as tool windows. They no longer create separate taskbar previews or Alt-Tab entries; the DPS meter remains the main CatScratch entry.
- Modules remain independently movable, with their existing saved positions and compact appearance. Upcoming and Started events continue to show their respective entries separately on screen.
- Overlay windows no longer expose native resize/maximize frame styles used by Windows Snap, preventing Windows from offering these overlays as normal fullscreen or docked windows when arranging them.
- Existing in-app resize grips, click-through, opacity and show/hide-all controls remain available.
- Added checks against actual application window handles to verify tool-window registration and the absence of Snap frame styles. The complete core test suite and release checks run before package publication.

## CatScratch v2.5.4

### Separate Started events module
- Started entries leave Upcoming events and appear exclusively in the new movable Started events module. While an occurrence is active, later occurrences of the same event are also hidden from Upcoming.
- Normal events show **Open!** with a three-minute countdown. Spacetime Rift shows **Open!** with a ten-minute countdown. Bosses show **Spawned** for two minutes without a countdown. Expired entries disappear automatically.
- Open Started events with the small clock button in the Upcoming header. Drag its header or cards to position it; its close button closes only that module.
- Both modules save position and open state independently, follow the compact meter styling and support click-through, event opacity and show/hide-all controls.
- Detached event cards use the same durations. Labels and help text are available in English, German, Russian and French.

### Group-search roster fixes
- Empty slots at the start or in the middle of a roster no longer hide the members after them. Later arrivals can fill those slots while other members remain visible.
- Confirmed leave notifications clear the roster even if the initial roster was missed. Invalid leave messages do not clear a valid group.
- These fixes were checked against supplied local recordings; private captures and player data are not included in the release.

### Reliable CP and GS identity
- Cached CP/GS are associated with name and server together. Same-named characters on different servers no longer share cached values, including after restarting.
- Reusing a player identifier for a different character clears stale gear and power values instead of showing them for the new character.
- Announcements with an unknown server no longer remove another confirmed character with the same name.
- Cache loading handles null, malformed and unreadable data more safely and filters invalid player records.

### Updates fully prepared before notification
- The update notice, notification and Restart to update button appear only after downloading, verifying and extracting the package successfully in the background.
- Clicking **Restart to update** applies the prepared package and restarts CatScratch. Restarting remains an explicit user action; failed verification or cancelled preparation does not show a ready notice.
- Automatic checks remain shortly after startup and every ten minutes when enabled. Manual checks remain available in Settings and the tray menu.
- Userdata is preserved. Backups, staging files and the archive are removed only after successful startup of the updated program; failed updates retain recovery files.
- When this promoted Stable build starts, Beta users automatically return to the Stable update channel. The displayed version is **v2.5.4**, without a beta suffix. A later manual Beta selection persists until the next Stable transition.

### Faster startup and ten patchnotes versions
- Game-data loading avoids unnecessary JSON copies. Local repeated-load measurements showed about 59% fewer allocations; this describes data loading rather than total cold-start time.
- Optional restored windows and checks are deferred until the first meter display. Redundant settings writes and synchronous download cleanup were reduced.
- Patch notes now show the ten newest releases in a scrollable sidebar on the left, with a clickable contents overview and detailed explanations per version.
- Each version shows its own changes without repeating older cumulative notes. Bundled history works offline; Refresh from GitHub updates notes for the selected channel.

### Release verification
- 334 automated core tests cover roster gaps, character identity, cache handling, event lifetime boundaries, release history and existing functionality.
- Before publication, the release pipeline also checks window interactions, shutdown, performance, event audio, boss records, background update preparation and direct-update userdata preservation.

## CatScratch v2.5.4-beta.4

### Started events module

- Move started entries out of Upcoming into a separate movable Started events module. Keep its position and open state independently; reopen it with the clock button in Upcoming events. Both modules support click-through and show/hide all windows.
- Normal events show Open! with a remaining three-minute countdown; Spacetime Rift shows Open! for ten minutes; bosses show Spawned for two minutes without a countdown. Expired entries are removed automatically.
- Avoid duplicate active-event names in Upcoming. Later recurring starts return there after the active occurrence expires. Keep the same timing and labels on detached scheduled-event cards.

## CatScratch v2.5.4-beta.3

### Easier updates and faster startup

- Keep automatic update download, verification and extraction in the background. Show the meter notification and restart action only after preparation finishes; one click restarts into the prepared version. Retain successful-start cleanup and user data preservation.
- Reduce startup data-loading allocations and defer optional windows, driver-help checks and old-update cleanup until after the main meter can appear. Keep automatic update timing and capture startup unchanged.
- Expand patch notes to the ten newest versions, with a scrollable left version list, per-version notes and clickable contents.

## CatScratch v2.5.4-beta.2

### Reliability fixes

- Keep CP and GS separate for same-named characters on different servers, including after cache reloads and delayed server identification.
- Clear stale stats when an actor ID is reassigned, and retain known players when another same-named player arrives without a server identity.
- Ignore incomplete name-cache entries instead of allowing them to break startup; handle unavailable cache files gracefully.
- Expand regression checks for malformed packets, player identity, cache recovery and displayed stats. Existing death recap, boss bests, recent players, windows, performance and update checks remain covered.

## CatScratch v2.5.4-beta.1

### Group-search fixes

- Read all occupied group-search slots even when empty slots occur in the middle. Keep names, CP and GS for members after a vacancy and preserve them when a new player fills it.
- Clear retained group stats on a confirmed leave notification even if the current packet reader has not received a roster yet. Reject malformed leave messages and ignore member updates after leaving.
- Validate the fix against the supplied join recording and regression tests for sparse groups. Damage and DPS calculations are unchanged.

## CatScratch v2.5.3

### Five releases in the patch notes window

- Add compact version tabs for the five newest published releases, sorted by version from newest to oldest. Each tab shows only the changes in that version. Omit older notes repeated in cumulative hotfix bodies because their versions have separate tabs. Each selection starts at the top of its own notes.
- Place a clickable contents overview at the top of each version. Select a topic to jump directly to its detailed explanation below.
- Show Stable releases for the Stable update channel. Beta also includes published beta versions, with final releases sorting after their betas. Ignore drafts, invalid version tags and duplicate versions.
- Keep the bundled release archive available offline. Refresh from GitHub loads the published history and updates the tabs while retaining the selected version when it remains among the newest five. If GitHub is unavailable, retain the displayed notes.
- Match the DPS meter's compact dark surfaces, turquoise selected tab, Segoe UI and purple paw. Place the version tabs in a compact vertical sidebar on the left, with contents and detailed notes on the right. Preserve scrolling, resizing and existing screenshot previews.

### Feedback cleanup

- Remove Boss personal bests and Last played with from the Feedback section. Access both through their dedicated trophy and player icons in the DPS meter. Existing personal records and recent-player data remain available.
- Keep Feedback focused on reporting bugs and requesting features. Update the access instructions and verify the dedicated meter buttons remain present.

### Group-search statistics

- In the live view outside a dungeon, allow the confirmed group-search room's player names, CP and GS to replace a completed previous fight. Previously the retained fight could prevent the room overview from appearing until the meter was reset.
- Preserve active-fight priority, explicitly selected fight results and the completed fight display inside a dungeon. Returning to Live restores the current room overview when available. Damage calculation and DPS tracking are unchanged.
- Display only group information actually received and confirmed by the meter. Missing group data remains unavailable; this fix addresses display priority and does not reconstruct packets the meter did not receive.

### Complete changes from v2.5.1 through v2.5.2

### Stable hotfix

- Remove Boss personal bests and Last played with from the Statistics menu. Use their dedicated trophy and player buttons in the DPS meter; both features and their saved data remain available.
- Keep Statistics focused on live, current-session and saved fights, including the All saved fights shortcut. Preserve the existing compact DPS meter styling.

### Complete v2.5.1 changes included

The following full v2.5.1 release notes are included again for this hotfix. Access instructions reflect the cleaned Statistics menu in v2.5.2.

### Permanent personal boss bests

- Add Boss personal bests for real dungeon bosses and world bosses. Open the view through the small trophy icon in the DPS meter.
- Browse and search the complete bundled boss catalog, including bosses for which no personal record has been achieved yet. Records are separate for each character, server and class.
- Permanently keep six independent personal bests per boss: highest DPS, most damage, strongest hit, most healing, fewest own deaths and least damage taken. Higher is better for the first four values; lower is better for deaths and incoming damage.
- Each statistic can come from a different qualifying victory. Only an improved value replaces its previous record; equal or worse results leave it intact. Each record retains its achievement date.
- Store best values independently of saved fight history. The ten-saved-fight limit does not apply to boss records, and restarting or updating CatScratch preserves them. Automatic fight saving is not required.
- Require a confirmed boss kill, a known starting HP baseline, boss-only damage and usable own-character data. Test fights, recorded-capture playback and incomplete or unqualified attempts do not improve records. Observed capture loss also prevents a new record.
- Migrate eligible records from the earlier beta attempt format to permanent independent best values. Add observed death counts and death timestamps to fight records; repeated death notifications do not count again until the player is confirmed alive. Older fight files with unknown death data remain readable without inventing missing deaths.

### Last played with

- Add a local recent-teammates view, available from the player icon beside the trophy. List confirmed group members newest first, excluding the own character and unrelated nearby players.
- Keep the list separately for each own character/server and retain it across restarts. Repeated group appearances update an existing teammate instead of creating duplicates; there is no ten-player retention limit.
- Show the player name, class, server and last-played time in compact rows. Display the last received CP and GS directly in the row, without expanding it or showing equipment slots.
- Update CP and GS when new positive values arrive. Missing values do not erase known values. A dash means no usable value has been received; entries created before CP/GS storage cannot recover historical values that were never recorded.
- Add an individual copy-name icon for every player, plus character selection and player search. Stream mode uses the same alias for the displayed name and the copied name. Test mode and recorded captures do not populate the persistent list.

### Death recap and resurrection

- Add a compact automatic death recap showing received damage from the ten seconds before the own character dies. Include each attacker, skill, damage amount and time before death, together with attacker totals and total incoming damage.
- Keep the recap visible while dead and close it on fresh positive own HP or a map reload, including resurrection on the same map. Correct the HP packet classification that previously treated player HP as NPC-only data and left the recap on screen after revival. Decode HP inside batched stat updates as well, so resurrection without a map reload also dismisses the recap. Check complete batches before using their HP field; do not mistake unrelated resource values for health.
- Add a small tombstone icon to enable or disable the module. Match its proportions and stroke weight to the existing toolbar icons. The enabled state is saved.
- Add a configurable global shortcut for the module, default Shift+F4, under Settings > Hotkeys. Preserve the existing click-through and UI-scale behavior; attacker player names follow Stream mode.
- Keep the recap in memory only. It does not survive restarting CatScratch. Show an explicit empty state when no incoming hits were received, and flag truncated hit history under unusually high traffic. Continuing DoT ticks, pet actions and unrelated players do not count as proof of resurrection.
- Validate the resurrection corrections against two real recorded death/revival sequences, with and without a map reload, and add minimal packet regression tests. The third resurrection method has not yet been recorded or verified. Full recordings are not included in the release source.

### Background updates and cleanup

- Check the selected update channel shortly after startup and every ten minutes. Enable automatic checking and preparation by default, including a one-time migration for older settings; the option can be disabled under Settings > General > Updates.
- Download available verified packages in the background and check their reported size, SHA-256, product name and exact release identity before marking them ready. Extract them while the current meter continues running; do not replace locked program files or restart automatically.
- Show a compact update banner in the DPS meter. Once preparation completes, Restart to update applies the prepared package and restarts the meter after an explicit click. The update window also shows preparation progress and the restart action.
- Share an in-progress preparation between automatic and manual checks to avoid duplicate downloads. Cancel stale preparation when changing channels, skipping a release or disabling automatic updates. Failed validation never produces a restart-ready package.
- Remove old program backups, staged update files and the downloaded ZIP only after the new version successfully starts and acknowledges completion. Keep recovery backups if file replacement or startup fails. Preserve settings, saved fights, timers, boss bests and recent-player records.
- Track announcements and dismissed banners by the exact release tag, so one beta revision does not accidentally suppress another. Stable 2.5.1 supersedes the 2.5.1 betas. On first starting the promoted Stable build, Beta subscribers automatically return to the Stable update channel, including older installations without release tracking. A later deliberate choice of Beta is preserved on normal restarts; each future Beta-to-Stable promotion performs the return again.

### Reliability and presentation

- Improve TCP reconnection handling when the game reuses connection endpoints. Process a closing packet's final data, release ended connections and prevent buffered data from a previous connection leaking into a new one.
- Harden packet reassembly against sequence-number wraparound, duplicate or overlapping retransmissions, large sequence gaps and unchecked buffer growth. Continue using passive capture without terminating or controlling the game.
- Handle incomplete, malformed or legacy fight-history files without preventing normal startup. Preserve compatibility with existing saved fights and the current ten-saved-fight retention policy.
- Match the new views and controls to the DPS meter's compact dark surfaces, turquoise accents, class glyphs, Segoe UI text and purple paw branding. Reduce the trophy icon to the same visual scale as the other toolbar buttons.
- Add English, German, Russian and French labels for the new controls and preserve Stream mode, configurable shortcuts and click-through behavior.
- Pass 306 core tests plus the release checks for interaction, shutdown, performance, event audio, update channels, boss records, recent-player controls, background update preparation and the full update/restart/cleanup path.

## CatScratch v2.5.2

### Stable hotfix

- Remove Boss personal bests and Last played with from the Statistics menu. Use their dedicated trophy and player buttons in the DPS meter; both features and their saved data remain available.
- Keep Statistics focused on live, current-session and saved fights, including the All saved fights shortcut. Preserve the existing compact DPS meter styling.

### Complete v2.5.1 changes included

The following full v2.5.1 release notes are included again for this hotfix. Access instructions reflect the cleaned Statistics menu in v2.5.2.

### Permanent personal boss bests

- Add Boss personal bests for real dungeon bosses and world bosses. Open the view through the small trophy icon in the DPS meter.
- Browse and search the complete bundled boss catalog, including bosses for which no personal record has been achieved yet. Records are separate for each character, server and class.
- Permanently keep six independent personal bests per boss: highest DPS, most damage, strongest hit, most healing, fewest own deaths and least damage taken. Higher is better for the first four values; lower is better for deaths and incoming damage.
- Each statistic can come from a different qualifying victory. Only an improved value replaces its previous record; equal or worse results leave it intact. Each record retains its achievement date.
- Store best values independently of saved fight history. The ten-saved-fight limit does not apply to boss records, and restarting or updating CatScratch preserves them. Automatic fight saving is not required.
- Require a confirmed boss kill, a known starting HP baseline, boss-only damage and usable own-character data. Test fights, recorded-capture playback and incomplete or unqualified attempts do not improve records. Observed capture loss also prevents a new record.
- Migrate eligible records from the earlier beta attempt format to permanent independent best values. Add observed death counts and death timestamps to fight records; repeated death notifications do not count again until the player is confirmed alive. Older fight files with unknown death data remain readable without inventing missing deaths.

### Last played with

- Add a local recent-teammates view, available from the player icon beside the trophy. List confirmed group members newest first, excluding the own character and unrelated nearby players.
- Keep the list separately for each own character/server and retain it across restarts. Repeated group appearances update an existing teammate instead of creating duplicates; there is no ten-player retention limit.
- Show the player name, class, server and last-played time in compact rows. Display the last received CP and GS directly in the row, without expanding it or showing equipment slots.
- Update CP and GS when new positive values arrive. Missing values do not erase known values. A dash means no usable value has been received; entries created before CP/GS storage cannot recover historical values that were never recorded.
- Add an individual copy-name icon for every player, plus character selection and player search. Stream mode uses the same alias for the displayed name and the copied name. Test mode and recorded captures do not populate the persistent list.

### Death recap and resurrection

- Add a compact automatic death recap showing received damage from the ten seconds before the own character dies. Include each attacker, skill, damage amount and time before death, together with attacker totals and total incoming damage.
- Keep the recap visible while dead and close it on fresh positive own HP or a map reload, including resurrection on the same map. Correct the HP packet classification that previously treated player HP as NPC-only data and left the recap on screen after revival. Decode HP inside batched stat updates as well, so resurrection without a map reload also dismisses the recap. Check complete batches before using their HP field; do not mistake unrelated resource values for health.
- Add a small tombstone icon to enable or disable the module. Match its proportions and stroke weight to the existing toolbar icons. The enabled state is saved.
- Add a configurable global shortcut for the module, default Shift+F4, under Settings > Hotkeys. Preserve the existing click-through and UI-scale behavior; attacker player names follow Stream mode.
- Keep the recap in memory only. It does not survive restarting CatScratch. Show an explicit empty state when no incoming hits were received, and flag truncated hit history under unusually high traffic. Continuing DoT ticks, pet actions and unrelated players do not count as proof of resurrection.
- Validate the resurrection corrections against two real recorded death/revival sequences, with and without a map reload, and add minimal packet regression tests. The third resurrection method has not yet been recorded or verified. Full recordings are not included in the release source.

### Background updates and cleanup

- Check the selected update channel shortly after startup and every ten minutes. Enable automatic checking and preparation by default, including a one-time migration for older settings; the option can be disabled under Settings > General > Updates.
- Download available verified packages in the background and check their reported size, SHA-256, product name and exact release identity before marking them ready. Extract them while the current meter continues running; do not replace locked program files or restart automatically.
- Show a compact update banner in the DPS meter. Once preparation completes, Restart to update applies the prepared package and restarts the meter after an explicit click. The update window also shows preparation progress and the restart action.
- Share an in-progress preparation between automatic and manual checks to avoid duplicate downloads. Cancel stale preparation when changing channels, skipping a release or disabling automatic updates. Failed validation never produces a restart-ready package.
- Remove old program backups, staged update files and the downloaded ZIP only after the new version successfully starts and acknowledges completion. Keep recovery backups if file replacement or startup fails. Preserve settings, saved fights, timers, boss bests and recent-player records.
- Track announcements and dismissed banners by the exact release tag, so one beta revision does not accidentally suppress another. Stable 2.5.1 supersedes the 2.5.1 betas. On first starting the promoted Stable build, Beta subscribers automatically return to the Stable update channel, including older installations without release tracking. A later deliberate choice of Beta is preserved on normal restarts; each future Beta-to-Stable promotion performs the return again.

### Reliability and presentation

- Improve TCP reconnection handling when the game reuses connection endpoints. Process a closing packet's final data, release ended connections and prevent buffered data from a previous connection leaking into a new one.
- Harden packet reassembly against sequence-number wraparound, duplicate or overlapping retransmissions, large sequence gaps and unchecked buffer growth. Continue using passive capture without terminating or controlling the game.
- Handle incomplete, malformed or legacy fight-history files without preventing normal startup. Preserve compatibility with existing saved fights and the current ten-saved-fight retention policy.
- Match the new views and controls to the DPS meter's compact dark surfaces, turquoise accents, class glyphs, Segoe UI text and purple paw branding. Reduce the trophy icon to the same visual scale as the other toolbar buttons.
- Add English, German, Russian and French labels for the new controls and preserve Stream mode, configurable shortcuts and click-through behavior.
- Pass 306 core tests plus the release checks for interaction, shutdown, performance, event audio, update channels, boss records, recent-player controls, background update preparation and the full update/restart/cleanup path.

## CatScratch v2.5.1

Stable release of the features and fixes tested through v2.5.1-beta.5. Changes below are relative to Stable v2.5.0.

### Permanent personal boss bests

- Add Boss personal bests for real dungeon bosses and world bosses. Open the view through the small trophy icon in the DPS meter, Statistics > Boss personal bests, or Settings > General > Boss personal bests.
- Browse and search the complete bundled boss catalog, including bosses for which no personal record has been achieved yet. Records are separate for each character, server and class.
- Permanently keep six independent personal bests per boss: highest DPS, most damage, strongest hit, most healing, fewest own deaths and least damage taken. Higher is better for the first four values; lower is better for deaths and incoming damage.
- Each statistic can come from a different qualifying victory. Only an improved value replaces its previous record; equal or worse results leave it intact. Each record retains its achievement date.
- Store best values independently of saved fight history. The ten-saved-fight limit does not apply to boss records, and restarting or updating CatScratch preserves them. Automatic fight saving is not required.
- Require a confirmed boss kill, a known starting HP baseline, boss-only damage and usable own-character data. Test fights, recorded-capture playback and incomplete or unqualified attempts do not improve records. Observed capture loss also prevents a new record.
- Migrate eligible records from the earlier beta attempt format to permanent independent best values. Add observed death counts and death timestamps to fight records; repeated death notifications do not count again until the player is confirmed alive. Older fight files with unknown death data remain readable without inventing missing deaths.

### Last played with

- Add a local recent-teammates view, available from the player icon beside the trophy, the Statistics menu and Settings > General. List confirmed group members newest first, excluding the own character and unrelated nearby players.
- Keep the list separately for each own character/server and retain it across restarts. Repeated group appearances update an existing teammate instead of creating duplicates; there is no ten-player retention limit.
- Show the player name, class, server and last-played time in compact rows. Display the last received CP and GS directly in the row, without expanding it or showing equipment slots.
- Update CP and GS when new positive values arrive. Missing values do not erase known values. A dash means no usable value has been received; entries created before CP/GS storage cannot recover historical values that were never recorded.
- Add an individual copy-name icon for every player, plus character selection and player search. Stream mode uses the same alias for the displayed name and the copied name. Test mode and recorded captures do not populate the persistent list.

### Death recap and resurrection

- Add a compact automatic death recap showing received damage from the ten seconds before the own character dies. Include each attacker, skill, damage amount and time before death, together with attacker totals and total incoming damage.
- Keep the recap visible while dead and close it on fresh positive own HP or a map reload, including resurrection on the same map. Correct the HP packet classification that previously treated player HP as NPC-only data and left the recap on screen after revival. Decode HP inside batched stat updates as well, so resurrection without a map reload also dismisses the recap. Check complete batches before using their HP field; do not mistake unrelated resource values for health.
- Add a small tombstone icon to enable or disable the module. Match its proportions and stroke weight to the existing toolbar icons. The enabled state is saved.
- Add a configurable global shortcut for the module, default Shift+F4, under Settings > Hotkeys. Preserve the existing click-through and UI-scale behavior; attacker player names follow Stream mode.
- Keep the recap in memory only. It does not survive restarting CatScratch. Show an explicit empty state when no incoming hits were received, and flag truncated hit history under unusually high traffic. Continuing DoT ticks, pet actions and unrelated players do not count as proof of resurrection.
- Validate the resurrection corrections against two real recorded death/revival sequences, with and without a map reload, and add minimal packet regression tests. The third resurrection method has not yet been recorded or verified. Full recordings are not included in the release source.

### Background updates and cleanup

- Check the selected update channel shortly after startup and every ten minutes. Enable automatic checking and preparation by default, including a one-time migration for older settings; the option can be disabled under Settings > General > Updates.
- Download available verified packages in the background and check their reported size, SHA-256, product name and exact release identity before marking them ready. Extract them while the current meter continues running; do not replace locked program files or restart automatically.
- Show a compact update banner in the DPS meter. Once preparation completes, Restart to update applies the prepared package and restarts the meter after an explicit click. The update window also shows preparation progress and the restart action.
- Share an in-progress preparation between automatic and manual checks to avoid duplicate downloads. Cancel stale preparation when changing channels, skipping a release or disabling automatic updates. Failed validation never produces a restart-ready package.
- Remove old program backups, staged update files and the downloaded ZIP only after the new version successfully starts and acknowledges completion. Keep recovery backups if file replacement or startup fails. Preserve settings, saved fights, timers, boss bests and recent-player records.
- Track announcements and dismissed banners by the exact release tag, so one beta revision does not accidentally suppress another. Stable 2.5.1 supersedes the 2.5.1 betas. On first starting the promoted Stable build, Beta subscribers automatically return to the Stable update channel, including older installations without release tracking. A later deliberate choice of Beta is preserved on normal restarts; each future Beta-to-Stable promotion performs the return again.

### Reliability and presentation

- Improve TCP reconnection handling when the game reuses connection endpoints. Process a closing packet's final data, release ended connections and prevent buffered data from a previous connection leaking into a new one.
- Harden packet reassembly against sequence-number wraparound, duplicate or overlapping retransmissions, large sequence gaps and unchecked buffer growth. Continue using passive capture without terminating or controlling the game.
- Handle incomplete, malformed or legacy fight-history files without preventing normal startup. Preserve compatibility with existing saved fights and the current ten-saved-fight retention policy.
- Match the new views and controls to the DPS meter's compact dark surfaces, turquoise accents, class glyphs, Segoe UI text and purple paw branding. Reduce the trophy icon to the same visual scale as the other toolbar buttons.
- Add English, German, Russian and French labels for the new controls and preserve Stream mode, configurable shortcuts and click-through behavior.
- Pass 306 core tests plus the release checks for interaction, shutdown, performance, event audio, update channels, boss records, recent-player controls, background update preparation and the full update/restart/cleanup path.

## CatScratch v2.5.1-beta.5

- Fix death recap remaining visible after resurrection. Recognize player HP correctly and clear the recap when respawning on the same map.

## CatScratch v2.5.1-beta.4

- Add a compact death recap for the last 10 seconds of incoming damage. It appears on death and disappears after resurrection. Toggle it with the tombstone icon or a configurable hotkey (default Shift+F4).
- Show last known CP and GS directly in compact Last played with rows, alongside individual copy-name buttons.
- Check for updates every 10 minutes and prepare verified downloads in the background. Restart with one click when ready; remove old program files and update leftovers after successful startup.

## CatScratch v2.5.1-beta.3

- Match the trophy icon's size and stroke weight to the other DPS meter toolbar icons.

## CatScratch v2.5.1-beta.2

- Keep only permanent, independent best values for each dungeon and world boss. Fewer deaths and less damage taken are better; remove the ten-attempt view and add the complete bundled boss catalog.
- Add a trophy shortcut in the DPS meter and a Last played with menu showing recent teammates' names and classes, with a copy-name button per player.
- Match the DPS meter's style and preserve existing records, language support and Stream mode.

## CatScratch v2.5.1-beta.1

- Add boss-only personal records for DPS, damage, strongest hit and healing, plus own deaths with timestamps and damage taken per attempt.
- Open Boss personal bests from Statistics or Settings > General. Records stay local and separate for each character; test fights and recordings do not count.
- Improve reconnect handling, packet buffering and revival tracking. Fix startup problems caused by incomplete fight history files.
- Beta preview; Stable remains v2.5.0.

## CatScratch v2.5.0

- Add selectable Stable and Beta update channels in Settings > General. Stable advances to v2.5.0; beta builds are prereleases and never replace the stable download. Checking or switching channels never installs a release. Updates require Update now; returning to Stable preserves settings and saved fights.
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
