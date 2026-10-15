# CatScratch reliability review — 2026-10-15

Scope: current Beta after v2.5.4-beta.1. Functional source review and reproducible regression checks, not a guarantee that every possible in-game failure has been found.

## Findings corrected

- CP/GS lookup used character name alone. Characters with identical names on different servers could share values. The lookup and restored cache now use name plus server; server identity is assigned before applying gear.
- Reassigned actor IDs could retain the previous character's gear and displayed fight values. Clear those values on identity changes and refresh the displayed fields from the current identity.
- An announcement without a server could delete a known same-named player. Only deduplicate characters with a confirmed matching server.
- The name cache could deserialize null collections or null player/NPC records that later caused failures during startup. Reject unusable collections, filter unusable entries and tolerate unreadable cache files. Resolve relative save paths before creating their parent directory.

## Validation

- 325 automated core tests pass, including damage/healing, boss encounters, group membership, death recap, personal bests, recent teammates, storage, update channels and update packages.
- Additional deterministic malformed-packet coverage sends payload lengths 0–255 through every declared opcode reader; no unhandled exceptions observed.
- Seven application checks pass: interaction, shutdown, performance, event audio, release250, boss records and beta4 update preparation.
- Direct update test passes: user data retained, new application started and closed cleanly, acknowledged old backups and temporary packages removed.
- Four supplied packet recordings replay successfully: two resurrection recordings and both group-search recordings. Captures stay outside Git and release archives.
- Release workflow repeats the full core, application and direct-update checks for the published build.

## Remaining practical limits

- No live game control or fresh in-game group acceptance test was performed during this review.
- The third resurrection method still has no supplied recording; its exact packet sequence remains unverified.
- Synthetic performance measurements are not an in-game CPU/GPU benchmark.
- No changes to damage formulas, DPS calculation rules, meter styling or the automatic update timing were requested or made.
