# Party roster and quiet autostart review — 2026-10-16

## Reproduction

A locally supplied mid-combat recording contains three five-member roster packets. Before the fix, each emitted only four members. The final member has a database-ID server field different from the explicit born-server field, so the old CP anchor search rejected the record.

The parser now validates the complete born/current-server, role and CP shape in the existing bounded search. It prefers a matching database server and accepts a different explicit born server only with a plausible CP. The original database identity is retained for exact dungeon actor matching. Full-roster and incremental-member paths share the same logic.

After the fix, all three recorded roster packets emit five members with gear and power. The recording starts mid-session and does not contain fresh own identity, so an independent replay cannot prove the entire live fight display without earlier context. Synthetic tests cover full five-player damage tracking, different positions in the roster and exact database-identity matching. No private captures, player names or raw payload fixtures are included.

## Capture folder shortcut

Capture settings place Open captures folder next to Save recent debug using existing compact controls. The folder opener uses the existing application userdata/captures path, creates it when needed and passes it to Explorer using structured process arguments. English, German, Russian and French labels are included. The rendered settings panel was visually checked.

## Quiet optional watcher

A small GUI-subsystem helper starts PowerShell with UseShellExecute=false and CreateNoWindow=true, then exits. The helper uses the inbox Windows .NET Framework and does not keep meter binaries locked. Enabled installations migrate from the old direct PowerShell task when the updated meter starts. New and legacy watchers use separate config files so migration lets the old watcher stop without terminating child processes. A per-folder mutex prevents duplicate watchers.

The isolated process test confirms a zero console handle, one start per game session, a new start in the next session, no duplicate watcher and exit after disabling the config. It installs no startup tasks and does not interact with the user's running game or meter. Task XML is validated through Windows Task Scheduler without registration.

## Local validation

- 338 core tests passed.
- Seven runtime checks passed: interaction, shutdown, performance, event-audio, release250, boss-records and beta4.
- Quiet watcher process tests passed; no console was allocated.
- The supplied capture was replayed locally; all three rosters now contain five members.
- README contents anchors and the compact Capture settings render were checked.

## Direct historical statistics

Earlier session and saved-fight selections now open the existing breakdown window directly, using the selected fight and its own character by default. The overlay has no historical selection state and always follows Live. Window checks cover immediate opening, reusing the same session window, saved-fight opening and a new live fight updating the meter while old statistics retain their values. The title wheel opens the history picker without changing the meter; the saved-history action is labelled Open statistics in all four languages.
