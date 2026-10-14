# Release policy

Stable is v2.5.1. The project owner explicitly approved promoting the tested v2.5.1-beta.5 feature set to the Stable branch and release on 2026-10-14, with detailed patch notes covering all changes since v2.5.0. On 2026-10-15, the owner supplied the second resurrection capture and explicitly requested fixing that method before continuing this Stable promotion.

On 2026-10-15 the owner also requested that Beta users automatically return to the Stable update channel when a Beta is promoted. Apply that change when the promoted Stable build starts; preserve a deliberate later Beta selection during ordinary restarts. Preparing updates still never restarts the meter automatically.

Future updates must be published to the beta branch and marked as GitHub prereleases with `--latest=false` unless the owner explicitly requests Stable. Use tags such as `v2.5.1-beta.1`. Increment only the patch component unless instructed otherwise. Keep main and the latest stable release unchanged when publishing a beta.

Update checks run every 10 minutes and may download, verify and extract a selected-channel update in the background. Replacing running program files and restarting require an explicit Restart to update click. Delete the old backup, prepared payload and downloaded archive only after the new version successfully starts; keep recovery files on failure.

The owner authorized beta branch uploads and beta prerelease publication without further approval on 2026-10-14. Stable uploads and promotion require the owner's explicit approval.

The owner lifted the temporary upload hold and authorized all outstanding changes for the next Beta on 2026-10-14, including compact recent-player CP/GS, background updates and death recap. Stable still requires explicit approval.

Write brief, high-level patch notes for Beta and detailed patch notes for Stable. Add patch notes only for a publication, not for each intermediate change. Publish the matching version's notes as the release body.
