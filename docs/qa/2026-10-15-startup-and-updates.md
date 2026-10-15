# Startup and update flow — 2026-10-15

The automatic update path downloads, verifies and extracts the new version while the old meter keeps running. The meter banner and its restart button now require the prepared state, matching the existing tray notification. No prompt or restart action is shown during download or extraction. A restart click applies the prepared files; successful-start acknowledgement still controls backup cleanup. Periodic-check preferences and timings are unchanged.

Startup work: consume JSON values while their iterator-owned document remains alive instead of cloning every value; parse UTF-8 bytes directly. Run old-download cleanup on a background worker, and defer optional restored event/pet windows plus the Npcap help check until application idle, after the main window can paint. Capture starts at the same point as before.

## Measured data loading

Same machine, same bundled English tables, separate processes loading old/new Core assemblies. One warm-up then 20 measured loads per implementation:

| Metric | Before | After |
| --- | ---: | ---: |
| Median GameData.Load | 28.99 ms | 22.58 ms |
| Mean GameData.Load | 30.72 ms | 27.23 ms |
| Mean allocated bytes/load | 13,550,916 | 5,614,638 |

These are warmed data-loader measurements, not the full application cold-start time. Disk cache, runtime initialization and optional restored windows affect actual startup.

## Verification

326 core tests, including localization/fallback/override ownership after loading and ten-release history ordering. Application checks cover hidden banners while downloading, ready-only restart actions, invalid-digest rejection, channel changes, skipped releases, ten version tabs and normal existing UI features. The release workflow repeats all seven runtime checks plus the direct-update test with user data preservation.
