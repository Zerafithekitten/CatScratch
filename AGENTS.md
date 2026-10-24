# CatScratch project instructions

## Separate Light edition: explicit opt-in only

The owner commissioned CatScratch light on 2026-10-18 as an independent DPS-only public download on branch catscratch-light. Normal work continues only on the original CatScratch branches. Do not modify Light, merge changes into it, update its dependencies, rebuild it or release it unless the owner explicitly requests Light work. This applies even when fixing shared code in the full version. Light has its own executable/settings and manual light-vX.Y.Z downloads; never include it in original Stable/Beta update feeds or installers, and never replace the main Stable latest release with Light. Its initial release is light-v1.0.0.

## Required visual style

The DPS meter position pin must stay on the DPS combat display, at its upper right. Only the control panel's own position pin belongs in the panel. This is an explicit exception to moving general buttons into the panel; keep both pins independent.

The owner now requires the compact CatScratch control panel to be the permanent default, independent of the DPS combat display. Move the installed version and all general header/footer buttons into that panel. Keep its single Settings gear and the only in-app Exit action there; remove the tray Exit entry and all visible Exit buttons from the DPS display. Keep the panel clickable during global click-through, with independently saved position and pin state included in profiles. These instructions supersede older requirements below that located the gear, version and controls in the DPS meter. Preserve the combat rows and their layout.

Preserve the existing DPS meter layout. Use a crisp vector purple cat paw with four toes and a pad consistently, including the executable and tray icons. The owner prefers neutral dark gray backgrounds (#191D23 / #20242A), restrained gray borders (#414A56) and lavender #C49AF5 accents; do not tint general backgrounds purple or washed green. Preserve semantic class, healing and damage colors. Player rows stay narrow and retain class emblems, class-colored full-row background progress fills, DPS, percentage share and red incoming damage per player; omit total outgoing damage and the thin progress strip from the row. Other modules and central Settings may be visually refined within the meter's existing style. Central Settings use Save/Cancel for event, pet and combat options too; explicit profile-management and diagnostic actions remain immediate.

Keep one visible settings gear, in the DPS meter. Centralize event preferences and audio in Settings > Events and pet options in Settings > Pets; other modules must not expose their own settings gears. Show the installed version under the CatScratch title, keep the footer for controls and place the tombstone toggle first. Match functional icon sizes and spacing. Module pins block dragging only, preserve interactive controls and click-through, persist independently and participate in overlay profiles.

Updates and Feedback are independent Settings tabs, removed from General. Update channel and manual update checks belong in Settings > Updates; Feedback contains only the report controls.

The owner explicitly requires every CatScratch change, new window, menu, control and feature to match the current DPS meter. This preference is permanent. Reuse the meter's neutral dark gray surfaces, restrained lavender accents, Segoe UI typography, compact rounded panels, class glyphs and existing button/control styles. Use the purple cat paw branding. Do not introduce a separate visual design or native white controls. Shared resources for interactive meter tools belong in Theme/MeterTools.xaml.

The DPS meter must provide a small trophy icon that opens permanent personal boss bests. Last played with has its own view and a copy-name button for each teammate. Stream mode must mask displayed and copied names consistently.

Keep the Statistics menu focused on live, session and saved fights. Do not duplicate Boss personal bests or Last played with there; they have dedicated meter buttons.

The DPS meter always follows Live. Selecting any earlier session or saved fight opens its statistics immediately in a separate existing-style breakdown window, rather than switching the meter away from Live. Reuse an already open window for the same fight.

The Feedback section only contains feedback controls. Do not duplicate Boss personal bests or Last played with there either.

Patch notes show the ten newest releases in separate compact version tabs in a vertical sidebar on the left. Each tab shows only its own changes on the right, with a clickable contents overview above detailed explanations. Do not display repeated cumulative history in a tab when those versions have their own tabs.

Display modules must not create separate taskbar previews or Alt-Tab entries. Keep the DPS meter as the application entry; register auxiliary display windows as tool windows and exclude overlay frames from native Windows Snap while preserving custom placement and resize controls.

## Releases

Overlay profiles are optional named layouts managed in their own Settings tab. Keep capture, update channel, hotkeys and game-start preferences outside profiles. Boss comparisons use existing session/saved fights, match boss/map/character/outcome/damage mode, preserve unknown values and flag partial recordings. Do not enable automatic fight saving implicitly.

Death recap shows only the five latest received hits, newest first, with red damage values. Keep its compact dark CatScratch panel centered in the upper half of the meter screen by default, above standard resurrection controls. Preserve dragging and automatic hiding after confirmed revival.

Follow RELEASE-POLICY.md. Before any upload, show images of the actual changes and wait for the owner to explicitly approve the update. During development run only focused checks for the changed module; run the complete release checks only after approval. This owner instruction from 2026-10-19 supersedes earlier standing Beta-upload authorization. Stable publication requires explicit owner approval. Beta release notes are brief; Stable notes are detailed. Add release notes for a publication, not for every intermediate change.

Started events belong exclusively to the separate movable Started events module. Normal events show Open! and a remaining 3-minute countdown; Spacetime Rift uses 10 minutes; bosses show Spawned for 2 minutes without a countdown. Persist the module placement/open state independently from Upcoming events.

The Upcoming events clock opens a chronological 24-hour planner. Favorite event types persist independently of overlay selection and sounds; mark their occurrences and matching overlay cards with a thin, subtly glowing gold frame. Keep general surfaces gray and branding lavender.

Boss fight comparison automatically uses the last earlier matching recorded run. Show the boss name prominently on both run cards with date and duration underneath; do not expose record identifiers or a manual fight selector.

Interface accent, background and text colors are optionally customizable in Settings > Appearance > Interface colors. Preserve the gray/lavender defaults, vector paw branding and semantic class/combat/event/favorite colors. The picker and preview are local drafts; Save persists and updates all windows, Cancel discards, and Restore default colors resets only the draft. Keep transparent surfaces transparent. Low contrast is advisory: never block saving valid RGB values or silently reset a saved palette. Cached overlay brushes must remain connected to palette changes, including click-through toggles and Restore defaults.

Mario Party is a small optional Easter egg: a ? beside Restore default colors enables a continuous scrolling rainbow across general window backgrounds, with a seamless 12-second loop (double the original speed). Use continuous, seamless transitions rather than abrupt color steps. Save persists the mode; Cancel discards the draft; Restore default colors and Save disables it. Semantic class, combat, event and favorite colors remain unchanged.

Self healing and Healing others in the personal-combat panel mean HP-confirmed effective healing, excluding overhealing. Never substitute reported heal amounts for missing HP evidence. Mark incomplete confirmed subtotals with * and show a dash when healing occurred but none is confirmed; explain in a tooltip. Preserve raw healing/self-healing history fields and DPS semantics. Recognized instant group heal skills carried by damage packets must become HealEvent; spirit-link packets must not.

Settings Save applies and persists changes while keeping the same Settings window open. Only its X closes it and discards unsaved drafts; do not restore a closing Cancel button. Pet tracker width stays adjustable in Settings and using its own horizontal resize grip, preserving click-through and saved placement.

Pet recommendations may use a confirmed map ID and fresh, server-announced source NPCs without attacks. Distinguish regional farm records from actual nearby sightings, exclude max-level pets in all Pet tracker views, including the attacked-target fallback, retain cached-progress markers and unknown baselines, and clear location context on map/character/capture changes. Unvalidated map pixels must never be displayed as exact live distances. Keep the original attacked-pet fallback when no supported location is known.

CatScratch remains exclusively passive: do not add game-memory access for positioning. Pet recommendations are capped at five, prioritizing confirmed fresh nearby sources; do not describe them as the five closest by distance without reliable live position data.

On 2026-10-22 the owner required the upcoming integrated Altgard map module to temporarily hide every other display module while the in-game map is open, including DPS/personal combat, resources, dungeon entries, upcoming/started events, pets, checklist, boss/death overlays, Hidden Cube banners, the supporter companion and the control panel. Restore the previously visible instances when the map closes; do not enable previously hidden modules, save temporary hiding into settings/profiles, restart capture or stop background data processing. Separate confirmed map-open state from marker pose confidence: a short failed match or zoom animation must not flash the other modules back onto an open map. Treat game-map suppression independently from the user's global hide state and release it safely when the map feature stops. This is an integration requirement; the separate MapVisionPrototype is not yet connected to the installed CatScratch application. Scope remains Altgard only; Verteron is deferred. No upload without the owner's review approval.

The full-version Checklist is opt-in in Settings > Checklist, with 1–20 compact task rows and eleven editable daily/weekly starting examples. Preserve the transparent window and tinted row surfaces. Checkmarks reversibly strike through text; task text and completion auto-save and survive restart, row-count reductions and layout-profile application. Never put task text/completion into overlay profiles or add implicit daily/weekly resets. Respect the module pin, global click-through and global show/hide. Light remains unchanged.

The Checklist toggle belongs in the permanent control panel in both orientations, with its state highlighted and matching icon documented in Help & symbols. Use the remaining fourth-row slot in the vertical panel; do not increase panel height. Toggling visibility must preserve all task text and completion state.

Checklist checkboxes must remain clickable in global click-through mode; task text and surrounding space still pass clicks to the game. Do not take game keyboard focus when toggling completion.

The owner authorized reset countdowns with explicit opt-in automatic clearing on 2026-10-19. Use configurable UTC hour (default 07:00) and weekly day (default Wednesday); clear only daily slots 1–2 and weekly slots 3–11. Preserve task text and all Other tasks. Demo mode opens isolated display/analysis previews, preserving actual module activation and task/history data. ONLY BOSS is a third strict concrete-boss option; preserve existing modes. Defeat estimates use rolling boss HP loss and show no estimate for stale/insufficient data; server deadline comparisons must not claim confirmed enrage detection. No upload until visual approval.

DPS overlay startup must use the shared WindowPresentation screen-bounds constraint, matching manual dragging. Do not restore a separate WorkingArea clamp during SourceInitialized: it moves a bottom-edge overlay upward by the taskbar height on restart. Keep placement regression coverage for repeated startup and detached controls.

The owner refined Test mode on 2026-10-19: show only the seven screen modules plus Statistics and Boss comparison (nine previews), without click-through or movement locking. Preserve actual user positions; preserve DPS and Settings dimensions and Settings position. Do not auto-tile existing windows or reopen other menus. Find free space only for modules without a saved/live position. Existing event module geometry comes from the live TimerCards state. No upload before review approval.

The owner replaced the separate Update popup on 2026-10-19: Update! must replace the CatScratch title inside the existing control panel only while a ready update is available. Otherwise show CatScratch. Preserve version, title slot dimensions, all three panel layouts and the original update click action; do not create a separate notification window.

The Checklist shortcut is configurable in Settings > Hotkeys, default Shift+F3. Preserve user bindings and exclude them from layout profiles. The hidden checklist title must reserve its layout space in click-through mode so task coordinates and checkbox hit targets never shift.

On 2026-10-20 the owner made the personal 4.0.0 patch-note style permanent: write as the solo creator with a cat fursona, warmly and naturally, with a linked overview and clear feature sections. English is primary; German app language uses German notes and all other languages use English. Keep Beta notes proportionate and Stable notes detailed. Stable upgrades open their own notes once after restart; ordinary restarts and Beta upgrades do not.

Future patch notes should include useful example images for visible features and short GIF demonstrations when supported and helpful. Use synthetic/sample data; do not add decorative screenshots to nonvisual fixes.

Supporter packages are optional signed data archives, never executable plugins. Preserve existing free customization and gameplay features. Keep creator private signing keys and paid pack contents outside the public repository; retain the pinned public key so previously issued packages continue working. Patreon posts, prices and uploads require separate user authorization.

On 2026-10-21 the owner requested that Beta updates no longer refresh the GitHub project presentation. For Beta, publish the code and prerelease downloads with brief matching notes, but leave README, banners, screenshots and repository description unchanged. Continue updating that presentation for owner-approved Stable releases.

On 2026-10-22 the owner requested marker filters directly on the open map, without opening Settings. Keep this small map-only control surface clickable without activating the game or taking keyboard focus; preserve Settings live-preview rollback when a draft is open. A confirmed local live test found that CatScratch's own resource bars cover the Altgard heading (full-title score 0.514); the detector must bootstrap through partial own-HUD occlusion without globally excluding the HUD from screen recordings or reducing the generic title threshold. Keep passive capture, Altgard-only scope and review-before-upload requirements.

On 2026-10-22 the owner requested six collapsible map-filter groups (shared with Settings), and then removed material text from map markers: retain the distinct original material icons and material-preserving clustering. Mouse flicker was reported even with the map closed. A revised passive capture test without CAPTUREBLT and with small title-only polling while closed was reported flicker-free by the owner; its log also confirmed map opening, localization and closing. Keep this observation distinct from a full installed-release validation.

On 2026-10-24 the owner required a separate German Discord-ready patch-note text for every Stable update, so they can post it manually. Include it as a release download and keep each message within 2,000 UTF-16 characters. Do not post to Discord automatically.

On 2026-10-24 the owner explicitly removed the legacy checklist UI: retain only the new character/server planner, with no Plan switch or old slot-count controls. Keep legacy task data as backup only. Tasks must be removable through visible edit controls, with undo preserving their progress. This supersedes older checklist UI requirements above.
