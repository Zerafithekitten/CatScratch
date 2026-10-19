# Release policy

SUPERSEDING OWNER INSTRUCTION, 2026-10-19: Before every upload, show the owner images of the actual changes and wait for explicit update approval. While developing, check only the added or changed module. Run the full release checks only after that approval. Earlier blanket Beta-upload authorization below no longer permits publishing without this review. The pending Beta 11 workflow was cancelled when this instruction arrived.

On 2026-10-19 the owner explicitly approved promoting the complete 3.0.4 Beta feature set and the reviewed final preview-layout, centered mode-lettering and DPS placement corrections to Stable 3.0.4, with comprehensive patch notes covering all changes since 3.0.3. Update main/GitHub documentation and preserve the separate frozen Light edition.

On 2026-10-17 the owner explicitly authorized publishing the grouped, reordered Settings navigation directly to Stable as **3.0.3**, with detailed release notes.

On 2026-10-17 the owner explicitly authorized publishing the completed stability review corrections directly to Stable as **3.0.2**, with detailed release notes.

On 2026-10-17 the owner explicitly authorized promoting the complete v3.0.1-beta.25 stand to Stable as **3.0.1**. Publish detailed notes covering every change since Stable 3.0.0, with prominent Main CatScratch Panel and Pet Tracker Rework headings. Keep the automatic Beta-to-Stable update-channel transition.

On 2026-10-16 the owner explicitly authorized promoting the complete current Beta feature set to Stable as **3.0.0**, superseding the earlier planned 2.6.0 Stable version. First separate Updates and Feedback into their own central Settings tabs. Publish detailed Stable notes covering all changes since Stable 2.5.5, and retain the automatic Beta-to-Stable channel transition.

On 2026-10-16 the owner reviewed the local polish build and explicitly authorized publishing it as v2.6.0-beta.2 after error checks. General backgrounds stay neutral gray; purple is reserved for branding and restrained accents. Preserve class colors and full-row player progress fills. Stable remains unchanged.

On 2026-10-16 the owner explicitly requested the next version series 2.6.0, carrying all previous 2.5.6 Beta changes (including Beta 7) into the eventual Stable 2.6.0 release. Publish current work as 2.6.0-beta.1 for testing; Stable still requires separate approval. The temporary upload hold was explicitly lifted once this work is finished.

Stable is v2.5.5. On 2026-10-15 the owner explicitly authorized an immediate Stable hotfix for duplicate module taskbar entries and Windows Snap suggestions. On 2026-10-15 the owner explicitly authorized promoting the complete v2.5.4-beta.4 stand to Stable with detailed patch notes.

For v2.5.3, On 2026-10-15 the owner explicitly authorized publishing the five-release patch-note tabs, Feedback cleanup and group-search display fix to Stable, with detailed cumulative notes from v2.5.1 through this release.

For v2.5.2, on 2026-10-15 the owner explicitly approved a Stable hotfix removing the duplicate Boss personal bests and Last played with entries from Statistics. Publish its hotfix notes together with the complete v2.5.1 patch notes.

For v2.5.1, the project owner explicitly approved promoting the tested v2.5.1-beta.5 feature set on 2026-10-14, with detailed patch notes covering all changes since v2.5.0. On 2026-10-15, the owner supplied the second resurrection capture and explicitly requested fixing that method before completing that Stable promotion.

On 2026-10-15 the owner also requested that Beta users automatically return to the Stable update channel when a Beta is promoted. Apply that change when the promoted Stable build starts; preserve a deliberate later Beta selection during ordinary restarts. Preparing updates still never restarts the meter automatically.

Future updates must be published to the beta branch and marked as GitHub prereleases with `--latest=false` unless the owner explicitly requests Stable. Use tags such as `v2.5.1-beta.1`. Increment only the patch component unless instructed otherwise. Keep main and the latest stable release unchanged when publishing a beta.

Update checks run every 10 minutes and may download, verify and extract a selected-channel update in the background. Replacing running program files and restarting require an explicit Restart to update click. Delete the old backup, prepared payload and downloaded archive only after the new version successfully starts; keep recovery files on failure.

The owner authorized beta branch uploads and beta prerelease publication without further approval on 2026-10-14. Stable uploads and promotion require the owner's explicit approval.

The owner lifted the temporary upload hold and authorized all outstanding changes for the next Beta on 2026-10-14, including compact recent-player CP/GS, background updates and death recap. Stable still requires explicit approval.

Write brief, high-level patch notes for Beta and detailed patch notes for Stable. Add patch notes only for a publication, not for each intermediate change. Publish the matching version's notes as the release body.

On 2026-10-19 the owner explicitly requested Stable hotfix 3.0.5 for English AM/PM checklist reset times and weekdays localized to the selected application language, including the matching reset tooltips. Publish after checks; Light remains unchanged.

On 2026-10-19 the owner reviewed the boss estimate readability image and explicitly requested publishing it directly as Stable hotfix 3.0.6 after full checks. Keep Light unchanged.

On 2026-10-19 the owner reviewed all three inline Update title replacements and explicitly approved publishing them as Stable hotfix 3.0.7, with no separate notification popup and the normal CatScratch title when no prepared update exists.

On 2026-10-19 the owner explicitly authorized Stable hotfix 3.0.8 after adding the configurable Checklist hotkey and a full review. The owner additionally requested preserving Checklist task positions across click-through toggles. Show actual images before publication, run complete release checks, and leave Light unchanged.

On 2026-10-19 the owner reviewed the separate boss timing window image and explicitly authorized publishing it to Stable as v3.0.9. Run the complete release checks and keep Light unchanged.

On 2026-10-19 the owner resumed the held Stable hotfix with "mach erstmal den hotfix", authorizing v3.0.10 for the reviewed boss timing transparency correction and sound playback recovery. Exclude the separately discussed monster-picture suggestion and keep Light unchanged.

On 2026-10-19 the owner explicitly authorized publishing the opt-in click-through restart preference to Stable after verification as v3.0.11. Show the actual setting and run full release validation; keep Light unchanged.

On 2026-10-19 the owner reviewed the individual world boss settings image and explicitly authorized "upload beta" for v3.0.12-beta.1. Run complete release checks; publish only beta and its prerelease tag with latest=false. Keep main, Stable 3.0.11 and Light unchanged.

On 2026-10-19 the owner authorized "ab in die beta damit" for topmost recovery, then added restart click-through and missing transparent appearance to the same correction. Publish all verified fixes as v3.0.12-beta.2 with latest=false, preserving main, Stable 3.0.11 and Light.

On 2026-10-19 the owner explicitly requested manual-only world boss watching and direct Beta publication as v3.0.12-beta.3. Preserve existing watch choices, kill/respawn records, Stable and Light.

On 2026-10-19 the owner reviewed the Verteron cube alarm preview and explicitly requested Beta publication as v3.0.12-beta.4. Run full release validation; keep Stable and Light unchanged.

On 2026-10-19 the owner requested fixing click-through position shifts, especially Upcoming and Started events, in Beta. Actual before/after module images were shown; publish verified corrections as v3.0.12-beta.6, preserving Stable and Light.

The owner replaced the proposed cube sound with freesound_community-kitty-meow-85182.mp3, then explicitly resumed Beta publication on 2026-10-19. Include it only as the new-install default; preserve existing audio preferences. Publish these changes together with the reviewed geometry corrections as v3.0.12-beta.6.
