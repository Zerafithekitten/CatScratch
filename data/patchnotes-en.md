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
