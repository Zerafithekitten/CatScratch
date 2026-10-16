using AionMeter.Core.Events;

namespace AionMeter.Core.Combat;

/// <summary>Credits observed HP gains, never raw heal amounts. Ambiguous intervals remain unverified.</summary>
internal sealed class EffectiveHealing
{
    private const long FreshHpMs=1500,ConfirmationMs=250;
    private sealed record Pending(long At,uint Source,long Amount,Action<long>? Confirm);
    private sealed class Health
    {
        internal long Hp=-1,Max,At;
        internal bool Ambiguous;
        internal readonly List<Pending> Pending=[];
    }
    private readonly Dictionary<uint,Health> _health=[];
    private long _lastPrune;
    internal void Clear(){_health.Clear();_lastPrune=0;}
    internal void Prune(long time)
    {
        if(time-_lastPrune<1000)return;
        _lastPrune=time;
        foreach(var (actor,health) in _health.ToArray())
            if(time-health.At>FreshHpMs)_health.Remove(actor);
            else if(health.Pending.Any(h=>time-h.At>ConfirmationMs))
            {health.Pending.Clear();health.Ambiguous=true;}
    }
    internal void Remove(uint actor)=>_health.Remove(actor);
    internal void Damage(uint target){if(_health.TryGetValue(target,out var hp))hp.Ambiguous=true;}

    internal void Heal(long time,uint source,uint target,long amount,Action<long>? confirm)
    {
        if(amount<=0||!_health.TryGetValue(target,out var health)||time<health.At||time-health.At>FreshHpMs)return;
        if(health.Pending.Count>=32){health.Ambiguous=true;return;}
        health.Pending.Add(new(time,source,amount,confirm));
    }

    internal void Update(NpcHpEvent update)
    {
        if(update.IsNpc){Remove(update.ActorId);return;}
        if(!_health.TryGetValue(update.ActorId,out var health))
        {
            if(_health.Count>=4096)_health.Clear();
            _health[update.ActorId]=health=new();
        }
        if(update.TimeMs<health.At)return;
        var pending=health.Pending;
        if(pending.Count>0&&!health.Ambiguous&&health.Hp>0&&update.TimeMs-health.At<=FreshHpMs
            &&pending.All(h=>update.TimeMs>=h.At&&update.TimeMs-h.At<=ConfirmationMs)
            &&pending.Select(h=>h.Source).Distinct().Count()==1
            &&!(health.Max>0&&update.MaxHp>0&&health.Max!=update.MaxHp))
        {
            var gain=update.CurrentHp-health.Hp;
            var raw=pending.Sum(h=>h.Amount);
            var expected=health.Max>0?Math.Min(raw,Math.Max(0,health.Max-health.Hp)):raw;
            // No HP change is proven overhealing only when both observations confirm a full target.
            var full=health.Max>0&&health.Hp==health.Max&&update.CurrentHp==health.Max;
            // A smaller unrelated HP gain (e.g. natural regeneration) is not evidence for this heal.
            if(gain==expected&&(gain>0||full))
                foreach(var heal in pending)
                {
                    var credit=Math.Min(Math.Max(0,gain),heal.Amount);
                    heal.Confirm?.Invoke(credit);gain-=credit;
                }
        }
        pending.Clear();health.Ambiguous=false;health.Hp=update.CurrentHp;health.At=update.TimeMs;
        if(update.MaxHp>=update.CurrentHp&&update.MaxHp>0)health.Max=update.MaxHp;
        else if(update.CurrentHp>health.Max)health.Max=0;
    }
}
