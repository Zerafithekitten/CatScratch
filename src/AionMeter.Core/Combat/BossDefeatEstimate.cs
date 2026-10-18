namespace AionMeter.Core.Combat;

/// <summary>Rolling actual boss HP loss, including damage not broadcast to this client.
/// Does not use party totals, which may include adds. No projection through stale HP or healing.</summary>
public sealed class BossDefeatEstimate
{
    private readonly Queue<(long At,long Hp)> _samples=new();
    private (Guid Fight,uint Boss)? _key;
    private long _lastChange,_lastHp,_lastAt;
    public double? Update(EncounterSnapshot? fight,long now)
    {
        if(fight is not {IsActive:true,Boss:{Hp:>0} boss}){Reset();return null;}
        var key=(fight.Id,boss.ActorId);
        if(_key!=key || now<_lastAt || boss.Hp>_lastHp){Reset();_key=key;_lastChange=now;}
        if(boss.Hp!=_lastHp)_lastChange=now;
        _lastHp=boss.Hp;_lastAt=now;
        if(_samples.Count==0 || now-_samples.Last().At>=1000)_samples.Enqueue((now,boss.Hp));
        while(_samples.Count>1 && _samples.Peek().At<now-15000)_samples.Dequeue();
        var first=_samples.Peek();var elapsed=now-first.At;var lost=first.Hp-boss.Hp;
        if(elapsed<5000 || lost<=0 || now-_lastChange>=8000)return null;
        var seconds=(double)boss.Hp/lost*(elapsed/1000.0);
        return double.IsFinite(seconds) && seconds>0?seconds:null;
    }
    private void Reset(){_samples.Clear();_key=null;_lastHp=0;_lastAt=0;_lastChange=0;}
}
