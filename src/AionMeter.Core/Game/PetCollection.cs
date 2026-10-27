using AionMeter.Core.Events;
using AionMeter.Core.Protocol;

namespace AionMeter.Core.Game;

public sealed record PetProgress(string Name,int ServerId,long TimeMs,PetState[] Pets);
public sealed record PetState(int Id,int Level,int Souls)
{
    public int Needed(int goal)
    {
        goal=Math.Clamp(goal,1,3);
        if(Level>=goal) return 0;
        int[] costs=[5,25,75]; // Global: unlock, level 2, level 3; not KR's five-level progression.
        var remaining=Math.Max(0,costs[Math.Clamp(Level,0,2)]-Souls);
        for(var level=Level+1;level<goal;level++) remaining+=costs[level];
        return remaining;
    }
}
public sealed record PetCollectionEvent(long TimeMs,IReadOnlyDictionary<int,PetState> Pets):GameEvent(TimeMs);
public sealed record PetSoulEvent(long TimeMs,IReadOnlyDictionary<int,int> Added):GameEvent(TimeMs);
public sealed record PetSoulDropEvent(long TimeMs,uint NpcActorId,int ItemCode,int Amount):GameEvent(TimeMs);
public sealed record ActorPositionEvent(long TimeMs,uint ActorId,float X,float Y,float Z):GameEvent(TimeMs);
public sealed record ObservedPetFarm(int NpcCode,string Zone,string Faction);

/// <summary>Server-to-client layouts verified against the Global 2026-10-14 collection and 10→11 soul capture.</summary>
public static class PetPackets
{
    public static PetCollectionEvent? Collection(long time,ReadOnlySpan<byte> body)
    {
        var at=0;
        if(!Wire.TryU32(body,ref at,out _) || !Wire.TryVarint(body,ref at,out var count) || count>4096) return null;
        var pets=new Dictionary<int,PetState>();
        for(var i=0;i<count;i++)
        {
            if(!Wire.TryU32(body,ref at,out var id) || !Wire.TryU32(body,ref at,out var form) || !Wire.TryU32(body,ref at,out var level)
                || id is <1000 or >100000 || form is <1000 or >100000 || level>5 || pets.ContainsKey((int)id)) return null;
            pets[(int)id]=new((int)id,(int)level,0);
        }
        if(!Wire.TryVarint(body,ref at,out var soulsCount) || soulsCount>4096) return null;
        var seen=new HashSet<int>();
        for(var i=0;i<soulsCount;i++)
        {
            if(!Wire.TryU32(body,ref at,out var id) || !Wire.TryU32(body,ref at,out var souls) || id is <1000 or >100000 || souls>1000000 || !seen.Add((int)id)) return null;
            var existing=pets.GetValueOrDefault((int)id,new((int)id,0,0));
            pets[(int)id]=existing with {Souls=(int)souls};
        }
        // Remaining fields describe genus option rolls; deliberately not interpreted as pet progress.
        return new(time,pets);
    }
    public static PetSoulEvent? Souls(long time,ReadOnlySpan<byte> body)
    {
        var at=0;
        if(!Wire.TryVarint(body,ref at,out var count) || count is 0 or >4096 || body.Length-at!=count*8) return null;
        var entries=new Dictionary<int,int>();
        for(var i=0;i<count;i++)
        {
            if(!Wire.TryU32(body,ref at,out var id) || !Wire.TryU32(body,ref at,out var value) || id is <1000 or >100000 || value is 0 or >1000000 || !entries.TryAdd((int)id,(int)value)) return null;
        }
        return new(time,entries);
    }
}

/// <summary>Own session only. Deltas cannot invent a baseline; loading screens preserve a confirmed collection.</summary>
public sealed class PetCollection
{
    private readonly object _gate=new();
    private Dictionary<int,PetState> _pets=[];
    private string _name="";
    private int _server;
    private long _progressAt;
    private long _progressVersion;
    public long ProgressVersion {get{lock(_gate)return _progressVersion;}}
    public bool FromCache {get;private set;}
    public long ProgressTime {get{lock(_gate)return _progressAt;}}
    public PetProgress? SavedProgress(){lock(_gate)return Confirmed && _name.Length>0?new(_name,_server,_progressAt,_pets.Values.ToArray()):null;}
    public bool RestoreProgress(PetProgress cached)
    {
        if(cached.Pets is null || cached.Pets.Length>4096 || cached.TimeMs<=0 || cached.Pets.Any(p=>p is null || p.Id is <1000 or >100000 || p.Level is <0 or >3 || p.Souls is <0 or >1000000) || cached.Pets.Select(p=>p.Id).Distinct().Count()!=cached.Pets.Length)return false;
        lock(_gate)
        {
            if(Confirmed || _name!=cached.Name || _server!=cached.ServerId)return false;
            _pets=cached.Pets.ToDictionary(p=>p.Id);_progressAt=cached.TimeMs;_progressVersion++;Confirmed=true;FromCache=true;Revision++;
        }
        Changed?.Invoke();return true;
    }
    private PetCollectionEvent? _pending;
    private uint _self;
    private uint _attacked;
    private readonly List<int> _attackedCodes=[];
    public int[] AttackedNpcCodes(){lock(_gate)return _attackedCodes.ToArray();}
    private bool RememberAttack(uint id)
    {
        var changed=_attacked!=id;_attacked=id;
        if(_npcs.TryGetValue(id,out var npc) && (_attackedCodes.Count==0 || _attackedCodes[0]!=npc.NpcCode))
        {
            _attackedCodes.Remove(npc.NpcCode);_attackedCodes.Insert(0,npc.NpcCode);
            if(_attackedCodes.Count>16)_attackedCodes.RemoveAt(16);changed=true;
        }
        return changed;
    }
    public uint AttackedTargetId {get{lock(_gate)return _attacked;}}
    public int AttackedNpcCode {get{lock(_gate)return _npcs.TryGetValue(_attacked,out var npc)?npc.NpcCode:0;}}
    private ActorPositionEvent? _position;
    private int _mapId;
    public int MapId {get{lock(_gate)return _mapId;}}
    public void InvalidateLocation()
    {
        lock(_gate){_mapId=0;_position=null;_npcs.Clear();_dead.Clear();_drops.Clear();_attacked=0;_attackedCodes.Clear();Revision++;}
        Changed?.Invoke();
    }
    private readonly Dictionary<uint,NpcSeenEvent> _npcs=[];
    private readonly HashSet<uint> _dead=[];
    private readonly List<PetSoulDropEvent> _drops=[];
    private readonly Dictionary<int,List<ObservedPetFarm>> _farms=[];
    public long Revision {get;private set;}
    public bool Confirmed {get;private set;}
    public event Action? Changed;
    public IReadOnlyList<PetState> Snapshot() {lock(_gate) return _pets.Values.ToArray();}
    public IReadOnlyList<ObservedPetFarm> Farms(int pet) {lock(_gate) return _farms.TryGetValue(pet,out var sources)?sources.ToArray():[];}
    public Dictionary<int,ObservedPetFarm[]> FarmCache() {lock(_gate)return _farms.ToDictionary(p=>p.Key,p=>p.Value.ToArray());}
    public void RestoreFarms(IReadOnlyDictionary<int,ObservedPetFarm[]> sources)
    {
        lock(_gate)foreach(var (id,farms) in sources.Take(4096))
            if(id is >=1000 and <=100000)_farms[id]=(farms??[]).Where(f=>f is not null && f.NpcCode>0 && f.Zone is {Length:<=200} && f.Faction is "Elyos" or "Asmodians" or "Other").Distinct().Take(32).ToList();
    }
    // Client-announced NPC presence persists independently of movement timestamps.
    // The captured 0x4236 notification covers removal from the reported area too;
    // DeathEvent removes either case, and a new spawn permits actor-ID reuse.
    public (ActorPositionEvent? Own,IReadOnlyList<NpcSeenEvent> Npcs) Nearby() {lock(_gate)return(_position,_npcs.Where(n=>!_dead.Contains(n.Key)).Select(n=>n.Value).ToArray());}
    public void Observe(GameEvent value)
    {
        var changed=false;
        lock(_gate)
        {
            switch(value)
            {
                case SelfIdentifiedEvent self when self.Name.Length>0:
                    if(_name.Length>0 && (_name!=self.Name || _server!=self.ServerId)) {_pets.Clear();Confirmed=false;FromCache=false;_progressAt=0;_progressVersion++;_pending=null;_mapId=0;_position=null;_npcs.Clear();_dead.Clear();_drops.Clear();_attacked=0;_attackedCodes.Clear();changed=true;}
                    _name=self.Name;_server=self.ServerId;
                    _self=self.ActorId;
                    if(_pending is { } buffered){_pets=new(buffered.Pets);Confirmed=true;FromCache=false;_progressAt=buffered.TimeMs;_progressVersion++;_pending=null;changed=true;}
                    break;
                case PetCollectionEvent full:
                    if(_name.Length==0) {_pending=full;break;}
                    _pets=new(full.Pets);Confirmed=true;FromCache=false;_progressAt=full.TimeMs;_progressVersion++;changed=true;break;
                case PetSoulEvent delta when Confirmed:
                    _progressAt=delta.TimeMs;_progressVersion++;
                    foreach(var (id,amount) in delta.Added)
                    {
                        if(!_pets.ContainsKey(id) && _pets.Count>=4096)continue;
                        // Full collection lists omit unowned pets with no souls; a verified delta creates level 0.
                        var pet=_pets.GetValueOrDefault(id,new(id,0,0));
                        var souls=(int)Math.Min(1000000,(long)pet.Souls+amount);var level=pet.Level;int[] costs=[5,25,75];
                        while(level<3 && souls>=costs[level]){souls-=costs[level];level++;}
                        _pets[id]=pet with {Level=level,Souls=level>=3?0:souls};
                        var candidates=_drops.Where(d=>d.TimeMs<=delta.TimeMs && delta.TimeMs-d.TimeMs<1000 && d.Amount==amount).ToArray();
                        if(delta.Added.Count==1 && candidates.Length==1 && _npcs.TryGetValue(candidates[0].NpcActorId,out var npc))
                        {
                            var farm=new ObservedPetFarm(npc.NpcCode,"","Other");
                            if(!_farms.TryGetValue(id,out var sources))_farms[id]=sources=[];
                            if(sources.Count<32 && !sources.Contains(farm))sources.Add(farm);
                        }
                    }
                    changed=true;break;
                case ZoneChangedEvent zone:_mapId=zone.MapId;_position=null;_npcs.Clear();_dead.Clear();_drops.Clear();if(zone.MapId>0)_attackedCodes.Clear();changed=true;_attacked=0;break;
                case NpcSeenEvent npc:
                    if(_npcs.Count>=4096 && !_npcs.ContainsKey(npc.ActorId)){var oldest=_npcs.MinBy(x=>x.Value.TimeMs).Key;_npcs.Remove(oldest);_dead.Remove(oldest);}
                    _npcs[npc.ActorId]=npc;_dead.Remove(npc.ActorId);if(npc.ActorId==_attacked)changed=RememberAttack(npc.ActorId);break;
                case DamageEvent hit when _self!=0 && hit.SourceId==_self && hit.TargetId!=_self:
                    changed=RememberAttack(hit.TargetId);break;
                case HitNoticeEvent hit when _self!=0 && hit.ActorId==_self && hit.TargetId!=_self:
                    if(_attacked!=hit.TargetId){_attacked=hit.TargetId;changed=true;}break;
                case DeathEvent death:if(_npcs.ContainsKey(death.ActorId))_dead.Add(death.ActorId);break;
                case ActorPositionEvent pose when float.IsFinite(pose.X)&&float.IsFinite(pose.Y)&&float.IsFinite(pose.Z):
                    if(pose.ActorId==_self){if(_position is null||pose.TimeMs>=_position.TimeMs)_position=pose;}
                    else if(_npcs.TryGetValue(pose.ActorId,out var entity)&&pose.TimeMs>=entity.TimeMs)_npcs[pose.ActorId]=entity with {TimeMs=pose.TimeMs,X=pose.X,Y=pose.Y,Z=pose.Z};
                    break;
                case PetSoulDropEvent drop:
                    _drops.RemoveAll(d=>drop.TimeMs-d.TimeMs>2000);if(_drops.Count<32)_drops.Add(drop);break;
            }
            if(changed) Revision++;
        }
        if(changed) Changed?.Invoke();
    }
}
