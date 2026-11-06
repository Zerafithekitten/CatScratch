using AionMeter.Core.Events;
namespace AionMeter.Core.Game;

public sealed record PvpSnapshot(uint ActorId,string Name,GameClass Class,long? Hp,long? MaxHp,bool HpStale,long Outgoing,long Incoming,long LastCombatMs);

/// <summary>Only confirmed player-versus-player hits. No inferred faction, selected target, effects or rank.</summary>
public sealed class PvpCombat
{
    private sealed record Player(string Name,int Server,GameClass Class,long Seen);
    private readonly object _gate=new();
    private readonly Dictionary<uint,Player> _players=[];
    private readonly Dictionary<uint,(long Hp,long Max,long At)> _health=[];
    private HashSet<(string,int)> _party=[];
    private uint _self,_target;
    private long _outgoing,_incoming,_lastCombat,_targetAt;
    public void Reset(){lock(_gate){_players.Clear();_health.Clear();_party.Clear();_self=_target=0;_outgoing=_incoming=_lastCombat=_targetAt=0;}}
    public void ClearSession(){lock(_gate){_target=0;_outgoing=_incoming=_lastCombat=_targetAt=0;}}
    public void Observe(GameEvent value)
    {
        lock(_gate)
        {
            switch(value)
            {
                case ResourceSnapshotStartedEvent: Reset();break;
                case SelfIdentifiedEvent own:
                    if(_self!=own.ActorId)Reset();
                    _self=own.ActorId;break;
                case ZoneChangedEvent: _players.Clear();_health.Clear();_target=0;break;
                case PartyRosterEvent roster:
                    _party=roster.Members.Select(m=>(m.Name,m.ServerId)).ToHashSet();
                    if(!Opponent(_target))_target=0;break;
                case PartyLeftEvent: _party.Clear();break;
                case PlayerSeenEvent player:
                    if(_players.Count>=512&&!_players.ContainsKey(player.ActorId))
                    {var oldest=_players.MinBy(p=>p.Value.Seen).Key;_players.Remove(oldest);_health.Remove(oldest);if(_target==oldest)_target=0;}
                    _players[player.ActorId]=new(player.Name,player.ServerId,player.Class,player.TimeMs);break;
                case NpcSeenEvent npc: _players.Remove(npc.ActorId);_health.Remove(npc.ActorId);if(_target==npc.ActorId)_target=0;break;
                case SummonSeenEvent summon: _players.Remove(summon.ActorId);_health.Remove(summon.ActorId);if(_target==summon.ActorId)_target=0;break;
                case NpcHpEvent hp when !hp.IsNpc&&_players.ContainsKey(hp.ActorId)&&hp.CurrentHp>=0:
                    if(_health.TryGetValue(hp.ActorId,out var previous)&&hp.TimeMs<previous.At)break;
                    // Never retain a maximum from a different update or estimate HP by subtracting damage.
                    _health[hp.ActorId]=(hp.CurrentHp,hp.MaxHp>=hp.CurrentHp&&hp.MaxHp>0?hp.MaxHp:0,hp.TimeMs);break;
                case DeathEvent death:
                    _players.Remove(death.ActorId);_health.Remove(death.ActorId);
                    if(_target==death.ActorId||_self==death.ActorId)_target=0;break;
                case DamageEvent hit when _self!=0&&hit.Damage>0:
                    if(hit.SourceId==_self&&Opponent(hit.TargetId))
                    {_target=hit.TargetId;_targetAt=hit.TimeMs;_outgoing=Add(_outgoing,hit.Damage);_lastCombat=Math.Max(_lastCombat,hit.TimeMs);}
                    else if(hit.TargetId==_self&&Opponent(hit.SourceId))
                    {if(_target==0)_target=hit.SourceId;if(_target==hit.SourceId)_targetAt=hit.TimeMs;_incoming=Add(_incoming,hit.Damage);_lastCombat=Math.Max(_lastCombat,hit.TimeMs);}
                    break;
            }
        }
    }
    private static long Add(long a,long b)=>a>long.MaxValue-b?long.MaxValue:a+b;
    private bool Opponent(uint id)=>id!=_self&&_players.TryGetValue(id,out var p)&&!_party.Contains((p.Name,p.Server));
    public PvpSnapshot Snapshot(long now)
    {
        lock(_gate)
        {
            var active=Opponent(_target)&&now>=_targetAt&&now-_targetAt<=20000;
            var player=active?_players[_target]:null;
            var hp=active&&_health.TryGetValue(_target,out var h)?h:default;
            var fresh=hp.At>0&&now>=hp.At&&now-hp.At<=5000;
            return new(active?_target:0,player?.Name??"",player?.Class??GameClass.Unknown,fresh?hp.Hp:null,fresh&&hp.Max>0?hp.Max:null,!fresh&&hp.At>0,_outgoing,_incoming,_lastCombat);
        }
    }
}
