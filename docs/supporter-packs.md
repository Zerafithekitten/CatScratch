# Supporter packages (Stable 4.0.2+)

Settings > Patreon links to https://www.patreon.com/cw/deuzear_yuma and imports local `.cspack` data archives. Public app features, custom colors and existing custom audio remain available. Imported assets and opting in to the gold paw are independent from overlay profiles.

Import is an immediate content-management action. Choosing a theme, assigning sounds and showing the paw use Settings drafts and apply with Save. X discards later unsaved preferences but does not undo imported files.

Packages use format 1: `manifest.json`, `manifest.sig` and the WAV paths listed by the manifest. The manifest is UTF-8 JSON serialized with PascalCase fields. Its exact bytes are signed using ECDSA P-256 / SHA-256 and the .NET default P1363 signature format. A public key is pinned in the application; the creator's private key is never shipped.

The manifest declares Id, Name, Version, SupporterPaw, Themes (Name/Accent/Background/Text) and Sounds (Name/Path/Sha256). Only validated RGB hex theme colors and validated PCM/float WAV files up to ten seconds are accepted. No XAML, executable plugins, arbitrary extraction paths or unlisted entries are accepted. Bounded archive, expanded-byte, file-count and per-file limits apply before installation. Importing the same manifest is idempotent.

Installed original archives live under userdata/supporter-packs. Audio is materialized beneath a manifest fingerprint and revalidated when referenced by an installed sound ID. Files survive normal updates as user data. Nothing is downloaded automatically, and there is no subscription check or embedded creator token. A package signature establishes that its contents were issued by the creator; it does not establish an active Patreon membership or prevent sharing.

Supporter sound choices are available for Hidden Cubes, Pets, Resource warnings and the existing event audio picker. Import does not enable previously disabled sounds or change volume. Reopen Settings after importing to refresh the ordinary sound pickers; assignments can also be made directly in Patreon settings before Save.

The public repository contains importer code and a signing helper, not paid package assets or private signing material. Use `tools/supporter/Sign-SupporterPack.ps1` with a creator-controlled private key to issue packages. The initial download and English Patreon post drafts are kept in the creator's local delivery folder.
