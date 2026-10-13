using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace AionMeter.App.Services;

/// <summary>Real queue/failure/cancellation checks with in-memory HTTP responses; no network calls.</summary>
internal static class AssetDownloadChecks
{
    internal static void Run(string output,Action<bool,string> check)
    {
        var directory=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"asset-check-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var names=Enumerable.Range(1100,12).ToDictionary(i=>i.ToString(),_=>"test-"+Guid.NewGuid().ToString("N"));
            File.WriteAllText(Path.Combine(directory,"skill_icons.json"),JsonSerializer.Serialize(names));
            var handler=new FailedHandler();
            using(var icons=new SkillIcons(directory,true,handler))
            {
                foreach(var key in names.Keys) icons.Get(int.Parse(key)*10000);
                check(SpinWait.SpinUntil(()=>handler.Count==12 && handler.Active==0,5000),"All queued image requests finish using fake HTTP");
                for(var i=0;i<20;i++) foreach(var key in names.Keys) icons.Get(int.Parse(key)*10000);
                check(handler.Count==12 && handler.Maximum<=4,"Failed icon downloads back off and concurrency stays within four");
            }
            var bossHandler=new FailedHandler();
            using(var portraits=new BossPortraits(directory,true,bossHandler))
            {
                var boss=Random.Shared.Next(90_000_000,99_000_000);
                portraits.Get(boss);
                check(SpinWait.SpinUntil(()=>bossHandler.Count==1 && bossHandler.Active==0,5000),"Fake portrait failure completes");
                for(var i=0;i<20;i++) portraits.Get(boss);
                check(bossHandler.Count==1,"Failed portrait downloads back off without a permanent missing-image entry");
            }
            var blocked=new FailedHandler { DelayMs=10000 };
            using(var icons=new SkillIcons(directory,true,blocked))
            { foreach(var key in names.Keys) icons.Get(int.Parse(key)*10000); }
            check(SpinWait.SpinUntil(()=>AssetDownloads.Slots.CurrentCount==4,5000),"Disposal cancels active and queued downloads and releases every slot");
        }
        finally
        {
            // Only our freshly generated diagnostic directory; never follow a link.
            foreach(var file in Directory.GetFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
    private sealed class FailedHandler : HttpMessageHandler
    {
        internal int Count,Active,Maximum;
        internal int DelayMs=30;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Count);
            var active=Interlocked.Increment(ref Active);
            int previous;
            do { previous=Volatile.Read(ref Maximum); if(previous>=active) break; }
            while(Interlocked.CompareExchange(ref Maximum,active,previous)!=previous);
            try { await Task.Delay(DelayMs,cancellationToken).ConfigureAwait(false); return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable); }
            finally { Interlocked.Decrement(ref Active); }
        }
    }
}
