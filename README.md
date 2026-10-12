# Zerafi DPS Monitor

A separate always-on-top Windows damage meter for AION 2 by Zerafithekitten.

## Downloads

Get Setup or the portable ZIP from [Releases](https://github.com/Zerafithekitten/zerafi-dps-monitor/releases/latest).
Use Setup for automatic installation of updates. Portable copies offer downloads.
Install [Npcap](https://npcap.com/#download) separately, start the meter and teleport once.
Use borderless windowed mode to keep the meter visible above the game.

## Features

- Own damage while solo and confirmed current party members while grouped.
- Class, name, DPS, damage, share, Combat Power and Gear Score.
- Compact colored rows, resizing, opacity and shortcuts recorded by pressing keys.
- Click-through removes window frames while preserving damage, time and RTT.
- Movable event cards for the next selected start within an hour.
- Separate observed boss timers; history limited to ten completed fights.
- English interface and GitHub updates for installed copies.

The meter passively captures traffic. No game-memory access, injection, game-file
changes, outgoing game packets or simulated game input are implemented.
This does not imply publisher approval. Incoming packets can contain other players;
the results are filtered to your confirmed character and party.

## Limitations

Missing server data can make damage incomplete. RTT is a passive TCP estimate;
a local accelerator can expose only its local connection. Events use bundled
community schedules from [Shugo.GG](https://shugo.gg/timers), checked Oct 12, 2026.
Global event times are not officially confirmed. Verify availability in game.

## Development

Requires .NET 10. See [GITHUB-RELEASES.md](GITHUB-RELEASES.md).
Version tags build the Windows app and installer and publish release downloads.
Packet recordings and userdata are excluded from Git and releases.

## License

GPL-3.0, based on [cyberbadger6969/aion2-dps-meter](https://github.com/cyberbadger6969/aion2-dps-meter),
base revision 414de5c1d5f3d45ce35701bd25229c2dba736bb6.
Keep LICENSE and data/NOTICE.md with redistributions. Full source accompanies releases.
AION 2 belongs to NCSOFT.
