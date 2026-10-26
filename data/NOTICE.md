# Game data tables

The lookup tables in this folder are extracted from AION 2 game data (© NCSOFT) and were taken from
open-source meters licensed under GPL-3.0, which is why AionMeter as a whole is distributed under GPL-3.0
(see `../LICENSE`).

| File | Source |
|---|---|
| `skills/*.json`, `npcs/*.json`, `dungeons/*.json`, `dot_skill_ids.json`, `servers.json`, `skill_icons.json` | [taengu/A2Tools-DPS-Meter](https://github.com/taengu/A2Tools-DPS-Meter) `src/data`, commit `e32dc5e` (2026-10-05), GPL-3.0 |
| `healing_skill_ids.json` | [Kuroukihime/AIon2-Dps-Meter](https://github.com/Kuroukihime/AIon2-Dps-Meter) `GameData/Assets`, commit `2e8d67d` (2026-10-01), GPL-3.0 |
| `src/AionMeter.App/Assets/Classes/*.png` (class emblems; brawler downscaled to 128 px) | same repository, `AionDpsMeter.UI/Assets/Classes`, commit `2e8d67d`, GPL-3.0 |
| `boss_portraits.json` (NPC code → portrait file) | built from [MetaBot.GG's boss list](https://metabot.gg/en/aion-2/bosses) (2026-10-05): each dungeon boss's portrait file, matched to every NPC code with the same English name in `npcs/en.json`. The portraits themselves are not bundled: the app downloads them from MetaBot.GG at runtime (credit: [MetaBot.GG](https://metabot.gg)) |

Formats AionMeter reads (every file is optional):

- `skills/<lang>.json` — `{ "16040000": "Skill name", … }`
- `npcs/<lang>.json` — `{ "2000002": { "name": "…", "isBoss": false, "isDummy": false }, … }`
- `dungeons/<lang>.json` — `{ "600021": { "name": "Fire Temple" }, … }` (map id → name)
- `servers.json` — `{ "servers": { "2305": { "en": "…", "ru": "…" } } }`
- `dot_skill_ids.json`, `healing_skill_ids.json` — arrays of skill codes
- `field_boss_maps.json` — `{ "maps": { "1110": { "block": 2400, "en": "Altgard", "ru": "Альтгард" } } }`: a map's field boss
  list (opcode 01 91) holds its bosses in NPC-code order, all from one block of a thousand codes. Established on the
  2026-10-06 EU capture against the in-game list (24 of 24 slots).
- `boss_portraits.json` — `{ "portraits": { "2300471": "2300471", "2300104": "f/mob_berc_01", … } }` (file name on
  metabot.gg/web/aion2/npcs/ without `.webp`; NPCs not listed are tried under their own code)

Missing languages fall back to `en`.

## Pet collection catalog

`pets/catalog.json` contains factual pet IDs, localized proper names and genus labels adapted from the public [Gamers4Life pet database](https://gamers4.life/aion-2/database/en/pets/?tab=collection), checked 2026-10-14. Game names/data © NCSOFT. No site artwork, descriptive text, code or KR progression costs are included. Global progression is 5/25/75 souls through level 3, checked against captured Global collection/increment records and the [Mudthorn reference](https://wikily.gg/aion-2/pets/mudthorn). Initial farm-source mappings were checked against Global captures and that reference; other sources are learned conservatively from matched own soul loot.

Pet monster-source IDs and matching open-Global-zone presence were checked against [Aion 2 Maps Global pet data](https://aion2maps.com/map/assets/pets.json) and its [Global map index](https://aion2maps.com/map/assets/maps/index.json), exported from Global patch 20 (2026-09-28), retrieved 2026-10-14. Only factual pet/NPC IDs and zone names were retained; no map artwork, spawn coordinates, descriptions or site code are bundled. Taiwan-only source lists are excluded. A blank zone means no verified open-world location, not an inferred location.

Pet farming location facts in pets/locations.json were retrieved from https://aion2pets.app/pets.json on 2026-10-17, attributed there to Wikily (https://wikily.gg/aion-2/pets). Only matched existing pet IDs, monster/region names and recorded point coordinates are included, without artwork or site code. Region IDs (Verteron 1010, Altgard 1110, Chaotic Lower Reshanta 20) were cross-checked against https://aion2maps.com/map/assets/maps/index.json. Coordinates are map-space records, not validated live-player distances or guaranteed drops. Missing locations remain unknown.

## Resource icons

`src/AionMeter.App/Assets/Resources/*.png` are original AION 2 game icons (© NCSOFT), fetched without editing from the same official game-data CDN used by skill icons: `https://assets.playnccdn.com/static-aion2-gamedata/resources/` (2026-10-20). Bundled for reliable offline display. Item-name references were cross-checked with Aion2Data currency pages and Aion 2 Maps against the owner's screenshots.

| Resource | Original CDN filename |
| --- | --- |
| Odyle Energy | `Icon_Item_Odenergy_A_001.png` |
| Abyss Points | `Icon_abysspoint.png` |
| Centuryroot Token | `Icon_Item_Currency_Drop_Junk_A_r_076.png` |
| Daeva Pass Shop Coin | `icon_battlepass.png` |
| Phantasmal Fragment | `Icon_Usable_Potion_Skillpoint_002.png` |

Oath Coins: original NCSOFT `icon_season` game icon, retrieved from the [Oath Coin item entry](https://aion2maps.com/items/oath-coin/) / `https://aion2maps.com/map/assets/icons/items/icon_season.webp` on 2026-10-20. Converted losslessly from WebP to PNG for WPF display; no visual edits.


Altgard Pet farm-world facts in `pets/farm-world.json` are a filtered subset of the Global-client spawn records publicly served at https://aion2maps.com/map/assets/maps/World_D_A/mapdata.json (retrieved 2026-10-26). Only map/NPC identifiers and horizontal coordinates for existing catalog sources are included; no site code, artwork, descriptions or models are copied. Source XYZ-metre axes were converted back to wire XY world units (source x * 100, source z * 100). Axis/sign/scale were checked against three received Guard positions (66–191 world-unit horizontal differences, compatible with moving guards). These are potential farm locations, not confirmed live spawns or guaranteed drops; no route or travel distance is implied. Original game data belongs to NCSOFT.
