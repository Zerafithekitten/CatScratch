# Supporter packages (Stable 4.0.2+)

Settings > Patreon links to https://www.patreon.com/cw/Zerafithekitten and imports local `.cspack` data archives. Public app features, custom colors and existing custom audio remain available. Imported assets and opting in to the gold paw are independent from overlay profiles.

Import is an immediate content-management action. Choosing a theme, assigning sounds and showing the paw use Settings drafts and apply with Save. X discards later unsaved preferences but does not undo imported files.

Packages use format 1: `manifest.json`, `manifest.sig` and the WAV paths listed by the manifest. The manifest is UTF-8 JSON serialized with PascalCase fields. Its exact bytes are signed using ECDSA P-256 / SHA-256 and the .NET default P1363 signature format. A public key is pinned in the application; the creator's private key is never shipped.

The manifest declares Id, Name, Version, SupporterPaw, Themes (Name/Accent/Background/Text) and Sounds (Name/Path/Sha256). Only validated RGB hex theme colors and validated PCM/float WAV files up to ten seconds are accepted. No XAML, executable plugins, arbitrary extraction paths or unlisted entries are accepted. Bounded archive, expanded-byte, file-count and per-file limits apply before installation. Importing the same manifest is idempotent.

Installed original archives live under userdata/supporter-packs. Audio is materialized beneath a manifest fingerprint and revalidated when referenced by an installed sound ID. Files survive normal updates as user data. Nothing is downloaded automatically, and there is no subscription check or embedded creator token. A package signature establishes that its contents were issued by the creator; it does not establish an active Patreon membership or prevent sharing.

Supporter sound choices are available for Hidden Cubes, Pets, Resource warnings and the existing event audio picker. Import does not enable previously disabled sounds or change volume. Reopen Settings after importing to refresh the ordinary sound pickers; assignments can also be made directly in Patreon settings before Save.

The public repository contains importer code and a signing helper, not paid package assets or private signing material. Use `tools/supporter/Sign-SupporterPack.ps1` with a creator-controlled private key to issue packages. The initial download and English Patreon post drafts are kept in the creator's local delivery folder.

## Animated cat extension (Stable 4.0.5+)

A separate signed Cat Companion package may declare an optional Mascot object with Path, Sha256, FrameWidth, FrameHeight, Columns, Frames and FrameMilliseconds. Existing manifests without Mascot remain supported. The Cat Companion download contains no themes, sounds or supporter-paw entitlement and does not require the First Paws package.

The mascot asset is a single signed PNG sprite sheet under mascots/. The reader checks its hash, signature and dimensions before the app decodes it. Frames are 64–256 pixels per side, with 2–120 frames, 1–16 columns, 50–250 milliseconds per frame and a sheet limited to 2048×2048 pixels / 4 MB. All archive and unlisted-file restrictions still apply. The app validates the PNG decoder's actual dimensions before making the package available. Packages never supply executable animation code.

Settings > Patreon has an explicit Show animated CatScratch cat draft checkbox, disabled without its separate package. Importing does not enable the cat automatically or change the user's existing themes, sounds or paw. The transparent cat window is draggable, independently pinnable, excluded from the taskbar and follows global click-through and show/hide. Hidden controls keep their layout space. A dispatcher timer advances fixed frozen image frames only while visible; hiding or closing stops it. Position survives restart and participates in layout profiles; enabling the cat remains outside profiles.

The original art, rendering source, signed download, English member instructions and GIF preview stay in the creator's local Patreon folder. They are not bundled with the public source. Stable 4.0.5 adds this extension; earlier versions cannot import the cat-only package.

The cat has its own 50–200% size slider in Patreon settings, default 100%. Changes remain drafts until Save and survive restart. Only the art scales; header controls retain their normal size. Cat size and visibility stay outside layout profiles. Global UI scaling still applies independently.
