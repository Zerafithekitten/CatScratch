using AionMeter.Core.Events;

namespace AionMeter.Core.Game;

/// <summary>Verified own-coordinate notification; independent of other actors' movement.</summary>
public sealed record PetPositionAnchorEvent(long TimeMs,float X,float Y,float Z):GameEvent(TimeMs);
/// <summary>Own actor yaw in degrees in the game's XY plane. Emit only from a verified facing source, never velocity.</summary>
public sealed record PetFacingEvent(long TimeMs,uint ActorId,double Degrees):GameEvent(TimeMs);
public sealed record PetPositionEstimate(int MapId,double X,double Y,double Z,long AnchorMs,double UncertaintyUnits,double? FacingDegrees=null,long? FacingMs=null);
public sealed record PetTargetGuidance(uint ActorId,int NpcCode,double DistanceUnits,double BearingDegrees,long SeenMs,double UncertaintyUnits,double? RelativeBearingDegrees=null);

/// <summary>Short, conservative prediction from received own coordinates, never from map pixels or NPC averages.</summary>
public sealed class PetNavigation
{
    public const long FreshMs=1500,PredictionMs=750;
    private readonly object _gate=new();
    private uint _self;
    private int _map;
    private (string Name,int Server)? _identity;
    private ActorPositionEvent? _anchor;
    private PetPositionAnchorEvent? _pending;
    private double _vx,_vy,_vz;
    private bool _velocity;
    private PetFacingEvent? _facing;
    public void Reset(){lock(_gate){_self=0;_map=0;_identity=null;ClearAnchor();}}
    public void ClearPosition(){lock(_gate)ClearAnchor();}
    private void ClearAnchor(){_anchor=null;_pending=null;_velocity=false;_facing=null;}
    public void Observe(GameEvent value)
    {
        lock(_gate)
        {
            switch(value)
            {
                case ZoneChangedEvent zone:_map=zone.MapId;ClearAnchor();break;
                case ResourceSnapshotStartedEvent:_self=0;_identity=null;ClearAnchor();break;
                case SelfIdentifiedEvent own:
                    if(_self!=own.ActorId||_identity!=(own.Name,own.ServerId))
                    {
                        var pending=_pending;ClearAnchor();
                        if(pending is {} p&&own.TimeMs>=p.TimeMs&&own.TimeMs-p.TimeMs<=1000)_anchor=new(p.TimeMs,own.ActorId,p.X,p.Y,p.Z);
                    }
                    _self=own.ActorId;_identity=(own.Name,own.ServerId);_pending=null;break;
                case PetPositionAnchorEvent ownPosition:
                    if(_map<=0||!Valid(ownPosition.X,ownPosition.Y,ownPosition.Z)||_anchor is {} previous&&ownPosition.TimeMs<=previous.TimeMs)break;
                    _velocity=false;_facing=null;
                    if(_self!=0)_anchor=new(ownPosition.TimeMs,_self,ownPosition.X,ownPosition.Y,ownPosition.Z);else _pending=ownPosition;
                    break;
                case ActorPositionEvent pose when _self!=0&&pose.ActorId==_self:
                    if(!Valid(pose.X,pose.Y,pose.Z)||_anchor is {} newer&&pose.TimeMs<=newer.TimeMs)break;
                    _velocity=false;
                    if(_anchor is {} before)
                    {
                        var seconds=(pose.TimeMs-before.TimeMs)/1000d;
                        if(seconds is >=.05 and <=1.5)
                        {
                            var x=(pose.X-before.X)/seconds;var y=(pose.Y-before.Y)/seconds;var z=(pose.Z-before.Z)/seconds;
                            if(Math.Sqrt(x*x+y*y+z*z)<=2000){_vx=x;_vy=y;_vz=z;_velocity=true;}
                        }
                    }
                    _anchor=pose;break;
                case PetFacingEvent facing when _self!=0&&facing.ActorId==_self:
                    if(!double.IsFinite(facing.Degrees)||Math.Abs(facing.Degrees)>360||_facing is {} last&&facing.TimeMs<last.TimeMs)break;
                    _facing=facing with{Degrees=(facing.Degrees+360)%360};break;
            }
        }
    }
    public PetPositionEstimate? Snapshot(long nowMs)
    {
        lock(_gate)
        {
            if(_self==0||_map<=0||_anchor is not {} p||nowMs<p.TimeMs||nowMs-p.TimeMs>FreshMs)return null;
            var age=nowMs-p.TimeMs;var seconds=_velocity?Math.Min(age,PredictionMs)/1000d:0;
            var speed=_velocity?Math.Sqrt(_vx*_vx+_vy*_vy+_vz*_vz):0;
            var facing=_facing is {} f&&nowMs>=f.TimeMs&&nowMs-f.TimeMs<=FreshMs?f:null;
            return new(_map,p.X+_vx*seconds,p.Y+_vy*seconds,p.Z+_vz*seconds,p.TimeMs,50+age*(.2+speed/1000),facing?.Degrees,facing?.TimeMs);
        }
    }
    public static PetTargetGuidance? Nearest(PetPositionEstimate? own,int mapId,IEnumerable<NpcSeenEvent> npcs,IReadOnlySet<int> sources,long nowMs)
    {
        if(own is null||!double.IsFinite(own.X)||!double.IsFinite(own.Y)||!double.IsFinite(own.Z)||own.MapId!=mapId||mapId<=0||nowMs<own.AnchorMs||nowMs-own.AnchorMs>FreshMs||!double.IsFinite(own.UncertaintyUnits)||own.UncertaintyUnits is <0 or >500)return null;
        PetTargetGuidance? nearest=null;
        foreach(var npc in npcs)
        {
            if(npc.ActorId==0||!sources.Contains(npc.NpcCode)||nowMs<npc.TimeMs||nowMs-npc.TimeMs>FreshMs||!Valid(npc.X,npc.Y,npc.Z)||(npc.X==0&&npc.Y==0&&npc.Z==0))continue;
            var dx=npc.X-own.X;var dy=npc.Y-own.Y;var dz=npc.Z-own.Z;
            var distance=Math.Sqrt(dx*dx+dy*dy+dz*dz);
            var bearing=Math.Atan2(dy,dx)*180/Math.PI;
            double? relative=own.FacingDegrees is {} yaw&&double.IsFinite(yaw)&&own.FacingMs is {} at&&nowMs>=at&&nowMs-at<=FreshMs?
                RelativeBearing(bearing,yaw):null;
            if(nearest is null||distance<nearest.DistanceUnits)nearest=new(npc.ActorId,npc.NpcCode,distance,bearing,npc.TimeMs,own.UncertaintyUnits,relative);
        }
        return nearest;
    }
    /// <summary>UE-style XY: +X forward at yaw zero, +Y to its right. Display 90° means up/forward.</summary>
    public static double RelativeBearing(double worldBearing,double characterYaw)=>((90+characterYaw-worldBearing)%360+360)%360;
    private static bool Valid(float x,float y,float z)=>float.IsFinite(x)&&float.IsFinite(y)&&float.IsFinite(z)&&Math.Abs(x)<10000000&&Math.Abs(y)<10000000&&Math.Abs(z)<10000000;
}
