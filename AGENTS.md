# CatScratch project instructions

## Required visual style

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

Boss fight comparison automatically uses the last earlier matching recorded run. Show the boss name prominently on both run cards with date and duration underneath; do not expose record identifiers or a manual fight selector.
