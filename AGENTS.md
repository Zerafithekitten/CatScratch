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

Follow RELEASE-POLICY.md. Beta branch uploads and prereleases are authorized without further confirmation. Stable publication requires explicit owner approval. Beta release notes are brief; Stable notes are detailed. Add release notes for a publication, not for every intermediate change.

Started events belong exclusively to the separate movable Started events module. Normal events show Open! and a remaining 3-minute countdown; Spacetime Rift uses 10 minutes; bosses show Spawned for 2 minutes without a countdown. Persist the module placement/open state independently from Upcoming events.

The Upcoming events clock opens a chronological 24-hour planner. Favorite event types persist independently of overlay selection and sounds; mark their occurrences and matching overlay cards with a thin, subtly glowing gold frame. Keep general surfaces gray and branding lavender.

Boss fight comparison automatically uses the last earlier matching recorded run. Show the boss name prominently on both run cards with date and duration underneath; do not expose record identifiers or a manual fight selector.

Interface accent, background and text colors are optionally customizable in Settings > Appearance > Interface colors. Preserve the gray/lavender defaults, vector paw branding and semantic class/combat/event/favorite colors. The picker and preview are local drafts; Save persists and updates all windows, Cancel discards, and Restore default colors resets only the draft. Keep transparent surfaces transparent. Low contrast is advisory: never block saving valid RGB values or silently reset a saved palette. Cached overlay brushes must remain connected to palette changes, including click-through toggles and Restore defaults.

Mario Party is a small optional Easter egg: a ? beside Restore default colors enables a continuous scrolling rainbow across general window backgrounds, with a seamless 12-second loop (double the original speed). Use continuous, seamless transitions rather than abrupt color steps. Save persists the mode; Cancel discards the draft; Restore default colors and Save disables it. Semantic class, combat, event and favorite colors remain unchanged.

Self healing and Healing others in the personal-combat panel mean HP-confirmed effective healing, excluding overhealing. Never substitute reported heal amounts for missing HP evidence. Mark incomplete confirmed subtotals with * and show a dash when healing occurred but none is confirmed; explain in a tooltip. Preserve raw healing/self-healing history fields and DPS semantics. Recognized instant group heal skills carried by damage packets must become HealEvent; spirit-link packets must not.

Settings Save applies and persists changes while keeping the same Settings window open. Only its X closes it and discards unsaved drafts; do not restore a closing Cancel button. Pet tracker width stays adjustable in Settings and using its own horizontal resize grip, preserving click-through and saved placement.

Pet recommendations may use a confirmed map ID and fresh, server-announced source NPCs without attacks. Distinguish regional farm records from actual nearby sightings, exclude max-level pets in all Pet tracker views, including the attacked-target fallback, retain cached-progress markers and unknown baselines, and clear location context on map/character/capture changes. Unvalidated map pixels must never be displayed as exact live distances. Keep the original attacked-pet fallback when no supported location is known.

CatScratch remains exclusively passive: do not add game-memory access for positioning. Pet recommendations are capped at five, prioritizing confirmed fresh nearby sources; do not describe them as the five closest by distance without reliable live position data.
