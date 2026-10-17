# Stability review — 2026-10-17

Reviewed the current Stable 3.0.1 source and made the following corrections on `audit/stability-20261017`. These changes have not been released.

## Corrections

- Update checks now use the updater lifetime cancellation token. Shutdown cancels an outstanding request, suppresses late UI notifications and prevents another check or preparation after disposal. Repeated disposal is harmless.
- Release parsing ignores non-object entries and unusable download sizes, including overflowing numbers. Incorrect release-list shapes raise the JSON error already handled by callers. Invalid assets cannot become downloadable packages; valid release notes remain available.
- Restarting capture clears pending pet loot correlations together with location context. A reused NPC actor ID can no longer attach an old soul drop to a different monster. Confirmed collection progress remains intact.

## Verification

- All 384 core tests passed, including ten added regression cases.
- Release app build completed with zero warnings and zero errors.
- All 1,127 integrated interaction checks passed: Settings, theme application, modules, pins, click-through, profiles, pet display and window behavior.
- Release/update integration: 38 checks passed, including shutdown during an in-flight request and rejection of further checks after disposal.
- Boss records: 66 checks passed. Update preparation/channel cancellation/digest validation and related integration: 55 checks passed.
- Event audio validation passed all seven reported assertions.
- Shutdown left zero windows; retained pinned-card preferences as expected.
- Separate-process color persistence passed, including saved Mario Party mode.
- Isolated game companion test passed: no console allocated, exact executable matching, one launch per game session, relaunch on a new session and disabling the watcher. No startup task was installed.
- Synthetic refresh benchmark: 2,000 iterations in 185 ms; 17,720 allocated bytes per refresh. This measures party/event refresh logic, not packet capture or GPU rendering, and is not a real-game FPS measurement.

## Scope and limits

No game layout changes, game-memory access or automatic Stable publication. No private captures were uploaded. Existing parser, combat, party, healing, recap, history, event, pet and update tests were run; this does not prove that every unrecorded game packet or live-game situation is covered.

NuGet vulnerability metadata was unavailable from the configured source during the test run (NU1900); no claim of a completed dependency vulnerability scan is made. The test project retains three pre-existing CS0675 fixture warnings. The application build itself is clean. A live-game soak test and new installer round-trip were not performed in this review.
