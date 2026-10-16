# Release policy

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
