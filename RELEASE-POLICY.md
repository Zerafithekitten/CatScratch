# Release policy

Stable is v2.5.0, explicitly approved by the project owner on 2026-10-14.

Future updates must be published to the beta branch and marked as GitHub prereleases with `--latest=false` unless the owner explicitly requests Stable. Use tags such as `v2.5.1-beta.1`. Increment only the patch component unless instructed otherwise. Keep main and the latest stable release unchanged when publishing a beta.

Channel selection and update checks must never install an update automatically. Installation requires Update now.

The owner authorized beta branch uploads and beta prerelease publication without further approval on 2026-10-14. Stable uploads and promotion require the owner's explicit approval.

Write brief, high-level patch notes for Beta and detailed patch notes for Stable. Add patch notes only for a publication, not for each intermediate change. Publish the matching version's notes as the release body.
