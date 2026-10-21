using AionMeter.Core.Events;
using AionMeter.Core.Protocol;

namespace AionMeter.Core.Game;

public sealed record HiddenCubeSeenEvent(long TimeMs,uint ActorId,int TemplateId):GameEvent(TimeMs);
public sealed record HiddenCubeGoneEvent(long TimeMs,uint ActorId):GameEvent(TimeMs);
public readonly record struct HiddenCubeStatus(int ActiveCount,bool RecentlyGone);

/// <summary>Only explicitly confirmed object variants from approach/open recordings. No NPC or map-position guesses.</summary>
public static class HiddenCubePackets
{
    public const int ConfirmedTemplate=1301199;
    public const int VerteronTemplate=1200039;
    public static bool IsConfirmedTemplate(int template)=>template is ConfirmedTemplate or VerteronTemplate;
    public static HiddenCubeSeenEvent? Seen(long time,ReadOnlySpan<byte> body)
    {
        var at=0;
        if(!Wire.TryVarint(body,ref at,out var actor) || actor==0 || body.Length-at!=52)return null;
        if(body[at]!=4 || body[at+1]!=0 || !IsConfirmedTemplate((int)Wire.U32(body,at+2)))return null;
        for(var n=0;n<3;n++)if(!float.IsFinite(BitConverter.ToSingle(body.Slice(at+6+n*4,4))))return null;
        return new(time,actor,(int)Wire.U32(body,at+2));
    }
    public static HiddenCubeGoneEvent? State(long time,ReadOnlySpan<byte> body)
    {
        var at=0;
        return Wire.TryVarint(body,ref at,out var actor) && actor!=0 && body.Length-at==3
            && body[at]==4 && body[at+1]==3 && body[at+2]==0?new(time,actor):null;
    }
}

public sealed class HiddenCubes
{
    public const long LifetimeMs=60_000;
    public const long GoneNoticeMs=5_000;
    private readonly object _gate=new();
    private readonly Dictionary<uint,long> _active=[];
    private readonly HashSet<uint> _opened=[];
    private (string Name,int Server)? _own;
    private long _detectionVersion;
    private long? _goneAt;
    public long DetectionVersion {get{lock(_gate)return _detectionVersion;}}
    public void Reset(){lock(_gate){_active.Clear();_opened.Clear();_own=null;_goneAt=null;}}
    public void Observe(GameEvent value)
    {
        lock(_gate)
        {
            switch(value)
            {
                case ZoneChangedEvent: _active.Clear();_opened.Clear();_goneAt=null;break;
                case SelfIdentifiedEvent own:
                    var identity=(own.Name,own.ServerId);
                    if(_own is not null && _own!=identity){_active.Clear();_opened.Clear();_goneAt=null;}
                    _own=identity;break;
                case HiddenCubeSeenEvent cube when HiddenCubePackets.IsConfirmedTemplate(cube.TemplateId) && !_opened.Contains(cube.ActorId):
                    Expire(cube.TimeMs);
                    if(_active.Count<64 || _active.ContainsKey(cube.ActorId))
                    {
                        if(!_active.ContainsKey(cube.ActorId)){_detectionVersion++;_goneAt=null;}
                        _active[cube.ActorId]=cube.TimeMs;
                    }
                    break;
                case HiddenCubeGoneEvent gone:
                    Expire(gone.TimeMs);
                    if(_active.TryGetValue(gone.ActorId,out var seenAt)&&gone.TimeMs>=seenAt)
                    {
                        _active.Remove(gone.ActorId);_goneAt=gone.TimeMs;
                        if(_opened.Count<4096)_opened.Add(gone.ActorId);
                    }
                    break;
            }
        }
    }
    private void Expire(long now){foreach(var id in _active.Where(p=>now-p.Value>=LifetimeMs).Select(p=>p.Key).ToArray())_active.Remove(id);}
    public int Count(long now){lock(_gate){Expire(now);return _active.Count(p=>p.Value<=now);}}
    public HiddenCubeStatus Status(long now)
    {
        lock(_gate)
        {
            Expire(now);
            return new(_active.Count(p=>p.Value<=now),_goneAt is {} gone&&now>=gone&&now-gone<GoneNoticeMs);
        }
    }
}
