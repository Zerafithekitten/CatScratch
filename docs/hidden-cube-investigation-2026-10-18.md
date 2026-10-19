# Hidden cube prototype — 2026-10-18

Introduced in v3.0.4-beta.5 as an experimental, opt-in feature. Detection remains disabled until enabled under Settings > Hidden cubes. The DPS meter layout is unchanged.

## Confirmed protocol evidence

Two independently supplied Global captures show an interactive world object announced by `34 36`, template `1301199`, flags `04 00`, a 52-byte tail after the raw actor varint, and three finite position floats. Each has exactly one such announcement. The same raw actor subsequently appears in the interaction/opening sequence and a `35 36` state `04 03 00`.

Replaying the captures through the implementation produced:

| Capture | Packets | Announcements | Opening removals | Maximum active alerts |
| --- | ---: | ---: | ---: | ---: |
| First | 3515 | 1 | 1 | 1 |
| Second | 3509 | 1 | 1 | 1 |

Nearby generic world-object templates `1300865` and `1300891` are not classified as cubes. No broad NPC-name, object-family or position heuristic is used. Raw actor IDs remain in the world-object namespace and are not folded into combat IDs.

## Behavior

- One movable, pinnable, click-through-compatible window shows **HIDDEN CUBE DETECTED**. It does not take focus or add a taskbar entry.
- Duplicate announcements do not create duplicate windows. Opening removes the matching object; unrelated object states are ignored.
- Map changes, capture restart/disconnect and character changes clear the detector. Demo and imported fight replay do not create cube alerts.
- Unrefreshed announcements expire after 60 seconds. An announcement is evidence that the server sent the object, not proof of a measured distance or continued visual presence.
- Settings changes remain drafts until Save. Module enablement, placement and pin participate in overlay profiles. Closing the module via its X disables the feature; automatic removal leaves the preference enabled for later detections.
- Both captures lack a reliably identified, continuously updated own-player position in the existing passive decoder. Direction, distance, map placement and additional cube variants remain unsupported. No memory access or screenshot capture was introduced.

## Verification

391 core tests passed, including seven cube cases covering decoding, other object types, truncation, invalid positions, duplicates, opening, context resets and expiry. Both capture replays returned the counts above. The application and CLI build cleanly. All 1,145 interaction checks passed, including the new window, independent pin, click-through, default-off preference, profile round-trip and discarded drafts. Shutdown left zero windows, and 55 existing compatibility checks passed.

Private recordings and raw decoded diagnostics remain outside the repository. The alert requires a live-game test before its behavior can be considered verified beyond these captures. NuGet vulnerability metadata was unavailable during the local test run; the existing test-fixture compiler warnings are unchanged.


## Verteron extension — 2026-10-19 (Beta 3.0.12-beta.4)

The owner supplied an approach/open recording from the enemy side in Verteron. The previous detector emitted zero detections from its 3,670 packets. Its world-object announcement at 14:12:11.352 carries template `1200039`, object kind `04 00`, and the same 52-byte payload layout after the actor varint as the existing variant. At 14:12:30.852 the same actor receives the established `04 03 00` opened state. This matches the owner's reported find/open sequence.

Added only template `1200039` to the explicit allowlist alongside `1301199`. The decoder retains the actual template ID and the shared detector accepts both. This is evidence for the recorded Verteron variant, not a claim that every cube on that side is covered. No general object-family, map or faction heuristic was added. Alarm appearance, custom sound, expiry, duplicate suppression and context resets use the existing shared path.

Focused verification: 13 HiddenCubeTests pass, including the new variant's parser/alert/open/reannouncement lifecycle and rejected neighboring templates. Replaying the supplied Verteron recording now yields `seen=1, cleared=1, peak=1`. Both original approach/open captures still yield the same `1/1/1`. The app and CLI build with zero warnings/errors. Raw captures and actor/location data are not committed.
