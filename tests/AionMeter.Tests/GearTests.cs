using System.Text;
using AionMeter.Core;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Protocol;

namespace AionMeter.Tests;

/// <summary>Gear score ("GS") and combat power ("CP"): the party roster, the own record, and where they show.</summary>
public class GearTests
{
    [Theory]
    [InlineData(true,2305)][InlineData(false,0)]
    public void Dungeon_player_server_requires_exact_roster_database_identity(bool exact,int expected)
    {
        var events=new List<GameEvent>(); var parser=new PacketParser(GameData.Empty,events.Add);
        parser.Handle(Opcodes.PartyRoster,Roster([new("Self",2305,31,45,1900,85510,[0],[]),new("Mate",2305,28,45,1238,63529,[0],[])]));
        var body=new List<byte> {12,0,0,0,0,1,4}; body.AddRange("Mate"u8.ToArray());
        body.AddRange(BitConverter.GetBytes(28u)); body.AddRange(new byte[16]);
        body.AddRange(BitConverter.GetBytes((2305UL<<48)|(exact ? 1001UL : 9999UL)));
        parser.Handle(Opcodes.PlayerInfo,body.ToArray());
        var player=Assert.IsType<PlayerSeenEvent>(events[^1]); Assert.Equal(expected,player.ServerId);
        parser.Handle(Opcodes.PartyLeft,[0,0]);
        parser.Handle(Opcodes.PlayerInfo,body.ToArray());
        Assert.Equal(0,Assert.IsType<PlayerSeenEvent>(events[^1]).ServerId);
    }
    [Fact]
    public void Room_member_updates_removals_and_leave_refresh_the_roster()
    {
        var events = new List<GameEvent>(); var parser = new PacketParser(GameData.Empty, events.Add);
        parser.Handle(Opcodes.PartyRoster, Roster([new("Self", 2305, 31, 45, 1894, 85471, [0], [])]));
        var first = Assert.IsType<PartyRosterEvent>(events[^1]);
        Assert.Equal(600091, first.DungeonId); Assert.NotEqual(0u, first.PartyKey);
        var member = Roster([new("Mate", 2305, 24, 45, 945, 53718, [0], [])])[29..];
        member[1] = 2;
        var dbid = (2305UL << 48) | 1001UL;
        BitConverter.GetBytes(dbid).CopyTo(member, 2);
        parser.Handle(Opcodes.PartyMemberUpdate, member);
        var updated = Assert.IsType<PartyRosterEvent>(events[^1]);
        Assert.Equal(2, updated.Members.Count); Assert.Equal(53718, updated.Members[1].CombatPower);
        parser.Handle(Opcodes.PartyMemberRemoved, [.. BitConverter.GetBytes(dbid), 1]);
        Assert.Single(Assert.IsType<PartyRosterEvent>(events[^1]).Members);
        parser.Handle(Opcodes.PartyLeft, [0, 0]);
        Assert.IsType<PartyLeftEvent>(events[^2]);
        Assert.Empty(Assert.IsType<PartyRosterEvent>(events[^1]).Members);
        var count = events.Count; parser.Handle(Opcodes.PartyMemberUpdate, member);
        Assert.Equal(count, events.Count);
    }
    [Fact]
    public void Room_updates_require_an_existing_roster_and_a_valid_leave_payload()
    {
        var events = new List<GameEvent>(); var parser = new PacketParser(GameData.Empty, events.Add);
        var member = Roster([new("Mate", 2305, 24, 45, 945, 53718, [0], [])])[29..];
        parser.Handle(Opcodes.PartyMemberUpdate, member);
        Assert.Empty(events);
        parser.Handle(Opcodes.PartyRoster, Roster([new("Self", 2305, 31, 45, 1894, 85471, [0], [])]));
        var count = events.Count;
        parser.Handle(Opcodes.PartyLeft, [1, 0]); parser.Handle(Opcodes.PartyLeft, [0]);
        parser.Handle(Opcodes.PartyMemberUpdate, [0, 12, 1]);
        Assert.Equal(count, events.Count);
    }

    [Fact]
    public void Leave_without_a_roster_in_this_parser_clears_a_retained_group()
    {
        var preview = new PartyPreview();
        preview.Observe(new SelfIdentifiedEvent(0, 1, "Self", 2305, GameClass.Cleric));
        preview.Observe(new PartyRosterEvent(1, [new("Self",2305,45,2027,93503,GameClass.Cleric)],42,600001));
        Assert.Single(preview.Snapshot().Members);
        var events = new List<GameEvent>();
        var parser = new PacketParser(GameData.Empty, e => { events.Add(e); preview.Observe(e); });
        parser.Handle(Opcodes.PartyLeft, [1, 0]);
        parser.Handle(Opcodes.PartyLeft, [0]);
        Assert.Single(preview.Snapshot().Members);
        parser.Handle(Opcodes.PartyLeft, [0, 0]);
        Assert.IsType<PartyLeftEvent>(events[0]);
        Assert.Empty(Assert.IsType<PartyRosterEvent>(events[1]).Members);
        Assert.Empty(preview.Snapshot().Members);
        Assert.False(preview.Snapshot().InRoom);
        parser.Handle(Opcodes.PartyMemberUpdate, Roster([new("Old",2305,31,45,1000,50000,[],[])])[29..]);
        Assert.Equal(2, events.Count);
    }

    [Theory]
    [InlineData(0,1)]
    [InlineData(1,1)]
    [InlineData(1,2)]
    [InlineData(3,1)]
    public void Group_search_roster_keeps_members_after_empty_slots(int firstEmpty, int emptyCount)
    {
        var body = Roster([], vacant: 5)[..29].ToList();
        var expected = new List<string>();
        for (var slot = 0; slot < 5; slot++)
        {
            if (slot >= firstEmpty && slot < firstEmpty + emptyCount)
            {
                body.AddRange([0, (byte)(slot + 1), .. new byte[9]]);
                var tail = new byte[25]; tail[14] = 4;
                body.AddRange(tail);
                continue;
            }
            var name = slot == 4 ? "Self" : "Mate" + slot;
            expected.Add(name);
            var member = Roster([new(name,2305,31,45,(uint)(1800+slot),(ulong)(80000+slot),[],[0,1,1])])[29..];
            member[1] = (byte)(slot + 1);
            BitConverter.GetBytes((2305UL << 48) | (ulong)(1000 + slot)).CopyTo(member, 2);
            body.AddRange(member);
        }
        var preview = new PartyPreview();
        preview.Observe(new SelfIdentifiedEvent(0,1,"Self",2305,GameClass.Cleric));
        var events = new List<GameEvent>();
        var parser = new PacketParser(GameData.Empty, e => { events.Add(e); preview.Observe(e); });
        parser.Handle(Opcodes.PartyRoster, body.ToArray());
        var roster = Assert.IsType<PartyRosterEvent>(Assert.Single(events));
        Assert.Equal(expected, roster.Members.Select(m => m.Name));
        Assert.Equal(1804, roster.Members[^1].GearScore);
        Assert.Equal(80004, roster.Members[^1].CombatPower);
        Assert.True(preview.Snapshot().InRoom);
        Assert.Equal(expected.Count, preview.Snapshot().Members.Count);
        // A later join fills the vacancy without discarding members in higher slots.
        var joined = Roster([new("NewMate",2305,31,45,2000,90000,[],[])])[29..];
        joined[1] = (byte)(firstEmpty + 1);
        parser.Handle(Opcodes.PartyMemberUpdate, joined);
        Assert.Equal(expected.Count + 1, preview.Snapshot().Members.Count);
        Assert.Contains(preview.Snapshot().Members, m => m.Name == "Self");
    }
    [Theory]
    [InlineData(1889,1889,45,1889)]
    [InlineData(1889,2000,45,1889)]
    [InlineData(1889,1889,44,0)]
    [InlineData(1889,0,45,0)]
    public void Own_gear_reaches_solo_damage_without_a_party(uint current,uint highest,uint repeatedLevel,int expected)
    {
        var b = new List<byte> { 1,0,0,0,0,1,4 };
        b.AddRange("Self"u8.ToArray());
        b.AddRange(BitConverter.GetBytes((ushort)2305));
        b.AddRange(BitConverter.GetBytes(31u)); b.Add(2);
        b.AddRange(BitConverter.GetBytes(45u));
        b.AddRange(BitConverter.GetBytes(current)); b.AddRange(BitConverter.GetBytes(highest));
        b.AddRange(BitConverter.GetBytes(repeatedLevel));
        b.AddRange(new byte[32]); b.AddRange(BitConverter.GetBytes(85428UL));
        b.AddRange(BitConverter.GetBytes(85428UL)); b.AddRange(new byte[32]);
        var self=Assert.IsType<SelfIdentifiedEvent>(Assert.Single(Parse(Opcodes.SelfInfo,b.ToArray())));
        Assert.Equal(expected,self.GearScore);
        var tracker=new CombatTracker(GameData.Empty,new MeterOptions { PartyOnly=true,TargetMode=TargetMode.All });
        tracker.Process(self); tracker.Process(new DamageEvent(100,1,500,17010000,100,HitFlags.None));
        var row=Assert.Single(tracker.Snapshot(null,200)!.Combatants);
        Assert.Equal(expected,row.GearScore); Assert.Equal(85428,row.CombatPower);
    }
    private sealed record Member(string Name, ushort Server, uint Class, uint Level, uint Gear, ulong Power, byte[] Flags, byte[] Tail, ushort? DatabaseServer = null, ushort? CurrentServer = null);

    /// <summary>A <c>02 97</c> body laid out as documented (see PacketParser.ParsePartyRoster), vacant slots at the end.</summary>
    private static byte[] Roster(Member[] members, int vacant = 0)
    {
        var b = new List<byte> { 1, 2, 3, 4 };                    // party key
        b.Add(5);
        b.AddRange("Raids"u8.ToArray());                         // party name
        b.Add(5);                                                // party size
        b.AddRange(BitConverter.GetBytes(600091u));              // dungeon
        b.AddRange([0, 0]);
        b.AddRange(BitConverter.GetBytes((2305UL << 48) | 1));  // leader dbid
        b.AddRange([0, 0, 0]);
        b.Add((byte)(members.Length + vacant));                  // member count
        for (var i = 0; i < members.Length; i++)
        {
            var m = members[i];
            b.Add(0x2C);
            b.Add((byte)(i + 1));
            b.AddRange(BitConverter.GetBytes(((ulong)(m.DatabaseServer ?? m.Server) << 48) | (ulong)(1000 + i)));
            b.Add((byte)Encoding.UTF8.GetByteCount(m.Name));
            b.AddRange(Encoding.UTF8.GetBytes(m.Name));
            b.AddRange(BitConverter.GetBytes(m.Class));
            b.AddRange(BitConverter.GetBytes(m.Level));
            b.AddRange(BitConverter.GetBytes(m.Gear));
            b.AddRange(m.Flags);                                 // ready / login: not the same width for everyone
            b.AddRange(BitConverter.GetBytes(m.Server));         // born server
            b.AddRange(BitConverter.GetBytes(m.CurrentServer ?? m.Server)); // current server
            b.Add(0);                                            // party role
            b.AddRange(BitConverter.GetBytes(m.Power));
            b.AddRange(m.Tail);
        }
        for (var i = 0; i < vacant; i++) b.AddRange(new byte[11]);  // mask 0, slot, dbid 0, empty name
        return b.ToArray();
    }

    private static List<GameEvent> Parse(ushort opcode, byte[] body)
    {
        var events = new List<GameEvent>();
        new PacketParser(GameData.Empty, events.Add).Handle(opcode, body);
        return events;
    }

    [Fact]
    public void Party_roster_gives_every_members_gear_score_and_combat_power()
    {
        var body = Roster(
        [
            new("Sylvaen", 2305, 24, 45, 2859, 59_072, [0x01], [0x00, 0x00, 0x00]),
            // Wider flags and a ticket in the tail: combat power is still found past the server id, the next member by its header.
            new("Borgrim", 2305, 6, 45, 3012, 61_234, [0x00, 0x01], [0x01, 0x05, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00]),
            new("Квилл", 1102, 36, 44, 2650, 38_900, [0x01], [0x00, 0x00, 0x00]),
        ], vacant: 3);

        var roster = Assert.IsType<PartyRosterEvent>(Assert.Single(Parse(Opcodes.PartyRoster, body)));
        Assert.Equal(
        [
            new PartyMemberInfo("Sylvaen", 2305, 45, 2859, 59_072, GameClass.Elementalist),
            new PartyMemberInfo("Borgrim", 2305, 45, 3012, 61_234, GameClass.Gladiator),
            new PartyMemberInfo("Квилл", 1102, 44, 2650, 38_900, GameClass.Chanter),
        ], roster.Members);
    }

    [Theory]
    [InlineData(0)] [InlineData(2)] [InlineData(4)]
    public void Different_database_and_explicit_servers_keep_all_five_members(int movedSlot)
    {
        var members=Enumerable.Range(0,5).Select(i=>new Member("Player"+i,2305,24,45,1600,80000,[],[0,1,1])).ToArray();
        members[movedSlot]=new("Moved",2301,36,45,1438,75064,[],[0,1,1],DatabaseServer:2308,CurrentServer:4302);
        var events=new List<GameEvent>(); var parser=new PacketParser(GameData.Empty,events.Add);
        parser.Handle(Opcodes.PartyRoster,Roster(members));
        var roster=Assert.IsType<PartyRosterEvent>(Assert.Single(events));
        Assert.Equal(5,roster.Members.Count);
        var moved=roster.Members[movedSlot];
        Assert.Equal(("Moved",2301,1438,75064L),(moved.Name,moved.ServerId,moved.GearScore,moved.CombatPower));
        var tracker=new CombatTracker(GameData.Empty,new MeterOptions{PartyOnly=true,TargetMode=TargetMode.All});
        var self=roster.Members[0];
        tracker.Process(new SelfIdentifiedEvent(0,1,self.Name,self.ServerId,self.Class));
        tracker.Process(roster);
        for(var i=1;i<5;i++) { var player=roster.Members[i]; tracker.Process(new PlayerSeenEvent(1,(uint)i+1,player.Name,player.ServerId,player.Class)); }
        for(uint i=1;i<=5;i++) tracker.Process(new DamageEvent(10+i,i,500,17010000,100,HitFlags.None));
        Assert.Equal(5,tracker.Snapshot(null,20)!.Combatants.Count);
        // Dungeon identity is still verified by its exact original database ID, not inferred from a name.
        var identity=new List<byte>{12,0,0,0,0,1,5}; identity.AddRange("Moved"u8.ToArray());
        identity.AddRange(BitConverter.GetBytes(36u)); identity.AddRange(new byte[16]);
        identity.AddRange(BitConverter.GetBytes((2308UL<<48)|(ulong)(1000+movedSlot)));
        parser.Handle(Opcodes.PlayerInfo,identity.ToArray());
        Assert.Equal(2301,Assert.IsType<PlayerSeenEvent>(events[^1]).ServerId);
    }

    [Fact]
    public void Member_updates_accept_different_explicit_server_without_losing_other_members()
    {
        var events=new List<GameEvent>(); var parser=new PacketParser(GameData.Empty,events.Add);
        parser.Handle(Opcodes.PartyRoster,Roster([new("Self",2305,24,45,1900,90000,[],[])]));
        var updated=Roster([new("Moved",2301,36,45,1438,75064,[],[],DatabaseServer:2308,CurrentServer:4302)])[29..];
        updated[1]=2;
        parser.Handle(Opcodes.PartyMemberUpdate,updated);
        var roster=Assert.IsType<PartyRosterEvent>(events[^1]);
        Assert.Equal(2,roster.Members.Count);
        Assert.Equal((2301,75064L),(roster.Members[1].ServerId,roster.Members[1].CombatPower));
    }

    [Fact]
    public void Something_else_under_the_roster_opcode_gives_nothing()
    {
        Assert.Empty(Parse(Opcodes.PartyRoster, [1, 2, 3, 4, 200, 9, 9, 9]));
        Assert.Empty(Parse(Opcodes.PartyRoster, Enumerable.Range(0, 64).Select(i => (byte)(i * 37)).ToArray()));
    }

    [Fact]
    public void Own_record_ends_with_the_combat_power_pair()
    {
        var b = new List<byte> { 0xF8, 0x6A, 0, 0, 0, 0, 0x01, 7 };      // id 13688, mask1, mask2 (name follows), length
        b.AddRange("Sylvaen"u8.ToArray());
        b.AddRange([0x01, 0x09, 24, 0, 0, 0, 0, 45, 0, 0, 0]);         // server 2305, Elementalist, level 45
        b.AddRange(new byte[60]);
        b.AddRange(BitConverter.GetBytes(59_072UL));                    // current
        b.AddRange(BitConverter.GetBytes(59_072UL));                    // highest
        b.AddRange(new byte[32]);                                       // 48 bytes before the end, as on Global

        var self = Assert.IsType<SelfIdentifiedEvent>(Assert.Single(Parse(Opcodes.SelfInfo, b.ToArray())));
        Assert.Equal(("Sylvaen", 2305, GameClass.Elementalist, 45, 59_072L), (self.Name, self.ServerId, self.Class, self.Level, self.CombatPower));
    }

    [Fact]
    public void Gear_follows_players_by_name_across_zones_and_restarts()
    {
        var t = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        t.Process(new SelfIdentifiedEvent(0, 100, "Sylvaen", 2305, GameClass.Elementalist, 45, CombatPower: 59_072));
        t.Process(new PlayerSeenEvent(0, 101, "Borgrim", 2305, GameClass.Gladiator));
        t.Process(new PartyRosterEvent(0,
        [
            new("Sylvaen", 2305, 45, 2859, 59_500, GameClass.Elementalist),
            new("Borgrim", 2305, 45, 3012, 61_234, GameClass.Gladiator),
        ]));
        t.Process(new DamageEvent(1_000, 100, 500, 16040000, 1_000, HitFlags.None));
        t.Process(new DamageEvent(1_000, 101, 500, 11020000, 1_000, HitFlags.None));
        var snap = t.Snapshot(null, 2_000)!;
        Assert.Equal((2859, 59_500L), Gear(snap, "Sylvaen"));
        Assert.Equal((3012, 61_234L), Gear(snap, "Borgrim"));

        // A loading screen reissues ids; the roster's names still apply when the players are announced again.
        t.Process(new ZoneChangedEvent(30_000, 600021, "Fire Temple", true));
        t.Process(new PlayerSeenEvent(31_000, 202, "Borgrim", 2305, GameClass.Gladiator));
        t.Process(new DamageEvent(32_000, 202, 600, 11020000, 1_000, HitFlags.None));
        Assert.Equal((3012, 61_234L), Gear(t.Snapshot(null, 33_000)!, "Borgrim"));

        // And a restarted meter gets them back from the cache.
        var restarted = new CombatTracker(GameData.Empty, new MeterOptions { TargetMode = TargetMode.All });
        restarted.ImportCache(t.ExportCache());
        restarted.Process(new DamageEvent(40_000, 202, 600, 11020000, 1_000, HitFlags.None));
        Assert.Equal((3012, 61_234L), Gear(restarted.Snapshot(null, 41_000)!, "Borgrim"));
    }

    private static (int, long) Gear(EncounterSnapshot s, string name) =>
        s.Combatants.Single(c => c.Name == name) is var c ? (c.GearScore, c.CombatPower) : default;

    // ---------------------------------------------------------------- chat

    private static CombatantSnapshot Player(uint id, string name, long damage, int gear = 0, long power = 0) =>
        new(id, name, GameClass.Elementalist, false, damage, damage / 160.0, damage / 4_000_000.0, 300, 0.15, 34_520, 0, 2305, gear, power);

    private static EncounterSnapshot Fight(params CombatantSnapshot[] players) =>
        new(Guid.NewGuid(), "Balhash", null, DateTimeOffset.Now, 160_000, 160_000, false, EncounterEndReason.Kill, 4_000_000, 25_000,
            new BossSnapshot(500, 2701250, "Balhash", 0, 4_000_000), players);

    [Fact]
    public void Chat_names_gear_score_and_combat_power_in_brackets()
    {
        var fight = Fight(Player(1, "Sylvaen", 1_600_000, 2859, 59_072), Player(2, "Borgrim", 1_200_000, 3012, 61_234),
            Player(3, "Talwyn", 800_000, power: 47_796));
        Assert.Equal(
            "Balhash 2:40 KILL | Talwyn (CP 47.8K) #3 of 3: 5K/s, 800K dmg (20%), crit 15%, top hit 34.52K | party 25K/s",
            ChatLine.Player(fight, 3));
        Assert.Equal(
            "Balhash 2:40 KILL | 1.Sylvaen (GS 2859 / CP 59.1K) 10K/s 40% | 2.Borgrim (GS 3012 / CP 61.2K) 7.5K/s 30% | " +
            "3.Talwyn (CP 47.8K) 5K/s 20% | party 25K/s",
            ChatLine.Party(fight));
    }

    [Fact]
    public void Party_line_drops_the_gear_rather_than_players_when_it_does_not_fit()
    {
        var fight = Fight(Enumerable.Range(1, 5).Select(i => Player((uint)i, $"Player{i}", 800_000 - i, 3000 + i, 60_000 + i)).ToArray());
        var line = ChatLine.Party(fight);
        Assert.DoesNotContain("GS", line);
        Assert.Contains("5.Player5", line);
    }

    [Fact]
    public void Discord_table_gets_gs_and_cp_columns()
    {
        var expected = string.Join(Environment.NewLine,
            "**Balhash 2:40 KILL** | party 25K/s",
            "```",
            "#  Player      DPS  Damage  Share    GS     CP",
            "1  Sylvaen   10K/s    1.6M  40.0%  2859  59.1K",
            "2  Borgrim  7.5K/s    1.2M  30.0%     -      -",
            "```");
        Assert.Equal(expected, ChatLine.Table(Fight(Player(1, "Sylvaen", 1_600_000, 2859, 59_072), Player(2, "Borgrim", 1_200_000))));
    }
}
