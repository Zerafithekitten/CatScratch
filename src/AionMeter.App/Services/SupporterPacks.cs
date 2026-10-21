using System.IO;
using System.Security.Cryptography;
using AionMeter.Core.Supporter;
namespace AionMeter.App.Services;
internal sealed record InstalledSupporterPack(VerifiedSupporterPack Pack,string Directory)
{
    internal string? ArchivePath {get;init;}
    public string Label=>Pack.Manifest.Name+" · "+Pack.Manifest.Version;
    public override string ToString()=>Label;
}
internal sealed record SupporterSoundOption(string Id,string Label);
internal static class SupporterPacks
{
    internal const string PatreonUrl="https://www.patreon.com/cw/Zerafithekitten";
    internal const string PublicKey="""
-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE2nNt8U7kJyHzz3bHikZN7vnyCtM4
Yvy+hUuPqenS85/WpGTIfu67Gtt9A8+RQo5Za3GneMEq+p/eRDTpWKEiWA==
-----END PUBLIC KEY-----
""";
    private static readonly object Gate=new();
    private static List<InstalledSupporterPack>? _installed;
    private static string? _override;
    internal static string Root=>_override??Path.Combine(AppSettings.AppDataDir,"supporter-packs");
    private sealed class Restore(Action action):IDisposable{public void Dispose()=>action();}
    internal static IDisposable Isolate(string directory)
    {
        lock(Gate){var previous=_override;var catalog=_installed;_override=directory;_installed=null;return new Restore(()=>{lock(Gate){_override=previous;_installed=catalog;}});}
    }
    internal static IReadOnlyList<InstalledSupporterPack> All()
    {
        lock(Gate)
        {
            if(_installed is not null)return _installed.ToArray();
            _installed=[];
            if(!Directory.Exists(Root))return [];
            foreach(var path in Directory.EnumerateFiles(Root,"*.cspack").Take(32))
                try{var verified=SupporterPackage.Read(ReadArchive(path),PublicKey);_installed.Add(Materialize(verified) with {ArchivePath=path});}
                catch(Exception e)when(e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or CryptographicException or ArgumentException or NotSupportedException or FormatException){Log.Error("Supporter package was ignored",e);}
            return _installed.ToArray();
        }
    }
    private static byte[] ReadArchive(string path){var info=new FileInfo(path);if(info.Length<=0||info.Length>8_000_000)throw new InvalidDataException("Supporter package exceeds the size limit.");return File.ReadAllBytes(path);}
    internal static InstalledSupporterPack Import(string path)
    {
        var verified=SupporterPackage.Read(ReadArchive(path),PublicKey);
        lock(Gate)
        {
            All();var existing=_installed!.FirstOrDefault(p=>p.Pack.Fingerprint==verified.Fingerprint);if(existing is not null)return existing;
            if(_installed!.Count>=32)throw new InvalidDataException("The installed package limit has been reached.");
            Directory.CreateDirectory(Root);var target=Path.Combine(Root,verified.Fingerprint+".cspack");
            var temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try{File.WriteAllBytes(temp,verified.Archive);File.Move(temp,target,true);}finally{if(File.Exists(temp))File.Delete(temp);}
            var installed=Materialize(verified) with {ArchivePath=target};_installed.Add(installed);return installed;
        }
    }
    internal static void Remove(InstalledSupporterPack pack)
    {
        lock(Gate)
        {
            var installed=All().SingleOrDefault(p=>p.Pack.Fingerprint==pack.Pack.Fingerprint);
            if(installed is null)return;
            var root=Path.GetFullPath(Root);
            var archive=Path.GetFullPath(installed.ArchivePath??throw new InvalidDataException("Missing installed archive."));
            var audio=Path.GetFullPath(Path.Combine(root,"audio"));
            var directory=Path.GetFullPath(installed.Directory);
            if(!string.Equals(Path.GetDirectoryName(archive),root,StringComparison.OrdinalIgnoreCase)||
                !string.Equals(directory,Path.Combine(audio,installed.Pack.Fingerprint),StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Package removal must stay inside the installed package folder.");
            foreach(var path in new[]{root,audio,directory})
                if(Directory.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
                    throw new InvalidDataException("Package removal cannot follow a directory link.");
            File.Delete(archive);_installed!.Remove(installed);
            // Remove only files declared by the verified manifest; never recurse.
            foreach(var name in installed.Pack.Audio.Keys.Select(Path.GetFileName).Concat(installed.Pack.Manifest.Mascot is {} mascot?[Path.GetFileName(mascot.Path)]:Array.Empty<string>()))
                try{File.Delete(Path.Combine(directory,name!));}
                catch(Exception error)when(error is IOException or UnauthorizedAccessException){Log.Error("Removed package cache could not be cleared",error);}
            try{if(Directory.Exists(directory)&&!Directory.EnumerateFileSystemEntries(directory).Any())Directory.Delete(directory);}
            catch(Exception error)when(error is IOException or UnauthorizedAccessException){Log.Error("Removed package cache could not be cleared",error);}
        }
    }
    internal static void RepairRemovedReferences(InstalledSupporterPack removed,AppSettings settings,IEnumerable<EventSoundPreference>? events=null)
    {
        var replacements=removed.Pack.Manifest.Sounds.Select(sound=>
        {
            var replacement=All().SelectMany(p=>p.Pack.Manifest.Sounds.Select(s=>(Pack:p,Sound:s))).FirstOrDefault(x=>x.Sound.Sha256==sound.Sha256);
            return (Id:SoundId(removed,sound),Path:Path.Combine(removed.Directory,Path.GetFileName(sound.Path)),
                Replacement:replacement.Pack is null?"Chime":SoundId(replacement.Pack,replacement.Sound));
        }).ToArray();
        foreach(var sound in replacements)
        {
            if(settings.PetSound==sound.Id)settings.PetSound=sound.Replacement;
            if(settings.ResourceAlertSound==sound.Id)settings.ResourceAlertSound=sound.Replacement;
            if(string.Equals(settings.HiddenCubeSoundPath,sound.Path,StringComparison.OrdinalIgnoreCase))
                settings.HiddenCubeSoundPath=sound.Replacement=="Chime"?CustomEventAudio.DefaultCubeSound:ResolveSound(sound.Replacement);
            foreach(var entry in events??[])if(entry.Sound==sound.Id){entry.Sound=sound.Replacement;entry.FilePath=null;}
        }
        if(!HasPaw)settings.ShowSupporterPaw=false;
        if(CatPack is null)settings.ShowSupporterCat=false;
    }
    private static InstalledSupporterPack Materialize(VerifiedSupporterPack pack)
    {
        var directory=Path.Combine(Root,"audio",pack.Fingerprint);Directory.CreateDirectory(directory);
        foreach(var sound in pack.Audio)
        {
            var target=Path.Combine(directory,Path.GetFileName(sound.Key));
            if(File.Exists(target)&&new FileInfo(target).Length==sound.Value.Length&&SHA256.HashData(File.ReadAllBytes(target)).SequenceEqual(SHA256.HashData(sound.Value)))continue;
            File.WriteAllBytes(target,sound.Value);
        }
        if(pack.MascotImage is {} image && pack.Manifest.Mascot is {} mascot)
        {
            var target=Path.Combine(directory,Path.GetFileName(mascot.Path));
            // Decode on import/reload, before making the optional cat available.
            using(var stream=new MemoryStream(image))
            {
                var decoder=System.Windows.Media.Imaging.BitmapDecoder.Create(stream,System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                var frame=decoder.Frames[0];
                if(decoder.Frames.Count!=1||frame.PixelWidth!=mascot.Columns*mascot.FrameWidth||frame.PixelHeight!=((mascot.Frames+mascot.Columns-1)/mascot.Columns)*mascot.FrameHeight)throw new InvalidDataException("Invalid mascot image.");
            }
            if(!File.Exists(target)||!SHA256.HashData(File.ReadAllBytes(target)).SequenceEqual(SHA256.HashData(image)))File.WriteAllBytes(target,image);
        }
        return new(pack,directory);
    }
    internal static InstalledSupporterPack? CatPack=>All().Where(p=>p.Pack.Manifest.Mascot is not null).OrderByDescending(p=>Version.Parse(p.Pack.Manifest.Version)).FirstOrDefault();
    internal static bool HasPaw=>All().Any(p=>p.Pack.Manifest.SupporterPaw);
    internal static string SoundId(InstalledSupporterPack p,AionMeter.Core.Supporter.SupporterSound s)=>"supporter:"+p.Pack.Fingerprint+":"+Path.GetFileName(s.Path);
    internal static string? ResolveSound(string id)
    {
        if(!id.StartsWith("supporter:",StringComparison.Ordinal))return null;
        try
        {
            foreach(var p in All())foreach(var s in p.Pack.Manifest.Sounds)if(SoundId(p,s)==id)
            {
                var file=Path.Combine(p.Directory,Path.GetFileName(s.Path));
                if(!File.Exists(file)||new FileInfo(file).Length!=p.Pack.Audio[s.Path].Length||!Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).Equals(s.Sha256,StringComparison.OrdinalIgnoreCase))return null;
                return file;
            }
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){Log.Error("Supporter audio is unavailable",e);}
        return null;
    }
    internal static SupporterSoundOption[] SoundOptions()=>new[]{"Chime","Bell","Ping"}.Concat(SoundIds()).Select(id=>new SupporterSoundOption(id,SoundLabel(id))).ToArray();
    internal static string[] SoundIds()=>All().SelectMany(p=>p.Pack.Manifest.Sounds.Select(s=>SoundId(p,s))).ToArray();
    internal static string SoundLabel(string id)=>All().SelectMany(p=>p.Pack.Manifest.Sounds.Select(s=>(Id:SoundId(p,s),Name:s.Name))).FirstOrDefault(s=>s.Id==id).Name??id;
}
