using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using AionMeter.Core.Audio;
namespace AionMeter.Core.Supporter;

public sealed record SupporterTheme(string Name,string Accent,string Background,string Text);
public sealed record SupporterSound(string Name,string Path,string Sha256);
public sealed record SupporterMascot(string Path,string Sha256,int FrameWidth,int FrameHeight,int Columns,int Frames,int FrameMilliseconds);
public sealed record SupporterManifest(int Format,string Id,string Name,string Version,bool SupporterPaw,SupporterTheme[] Themes,SupporterSound[] Sounds,SupporterMascot? Mascot=null);
public sealed record VerifiedSupporterPack(SupporterManifest Manifest,byte[] Archive,string Fingerprint,IReadOnlyDictionary<string,byte[]> Audio,byte[]? MascotImage=null);
/// <summary>Signed data only: no XAML, executable code, or archive-controlled extraction paths.</summary>
public static class SupporterPackage
{
    public static VerifiedSupporterPack Read(byte[] archive,string trustedPublicKey)
    {
        static void Require(bool condition){if(!condition)throw new InvalidDataException("Invalid or unsupported supporter package.");}
        Require(archive.Length is >0 and <=8_000_000);
        using var zip=new ZipArchive(new MemoryStream(archive),ZipArchiveMode.Read);
        Require(zip.Entries.Count is >=2 and <=27);
        var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long total=0;
        foreach(var e in zip.Entries){Require(names.Add(e.FullName) && e.Length is >=0 and <=4_000_000);total+=e.Length;Require(total<=12_000_000);}
        byte[] ReadEntry(string path,int limit){var e=zip.GetEntry(path)??throw new InvalidDataException("Package content is missing.");Require(e.Length<=limit);using var input=e.Open();using var output=new MemoryStream();var buffer=new byte[8192];int count;while((count=input.Read(buffer))>0){Require(output.Length+count<=limit);output.Write(buffer,0,count);}return output.ToArray();}
        var json=ReadEntry("manifest.json",32_000);var signature=ReadEntry("manifest.sig",256);
        using var key=ECDsa.Create();key.ImportFromPem(trustedPublicKey);
        Require(key.VerifyData(json,signature,HashAlgorithmName.SHA256));
        var m=JsonSerializer.Deserialize<SupporterManifest>(json)??throw new InvalidDataException("Package manifest is missing.");
        Require(m.Format==1 && m.Id is not null && Regex.IsMatch(m.Id,"^[a-z0-9][a-z0-9-]{0,47}$") && m.Version is not null && Regex.IsMatch(m.Version,"^[0-9]{1,4}\\.[0-9]{1,4}\\.[0-9]{1,4}$"));
        bool Label(string? value)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=64&&!value.Any(char.IsControl);
        bool Color(string? value)=>value is not null&&Regex.IsMatch(value,"^#[0-9A-Fa-f]{6}$");
        Require(Label(m.Name)&&m.Themes is {Length:<=12}&&m.Sounds is {Length:<=12}&&(m.Themes.Length+m.Sounds.Length>0||m.SupporterPaw||m.Mascot is not null));
        Require(m.Themes!.All(t=>t is not null&&Label(t.Name)&&Color(t.Accent)&&Color(t.Background)&&Color(t.Text))&&m.Themes.Select(t=>t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()==m.Themes.Length);
        var audio=new Dictionary<string,byte[]>();
        foreach(var sound in m.Sounds!)
        {
            Require(sound is not null&&Label(sound.Name)&&sound.Path is not null&&Regex.IsMatch(sound.Path,"^sounds/[a-z0-9-]{1,48}\\.wav$")&&sound.Sha256 is not null&&Regex.IsMatch(sound.Sha256,"^[A-Fa-f0-9]{64}$"));
            var path=sound!.Path!;var bytes=ReadEntry(path,4_000_000);Require(audio.TryAdd(path,bytes));
            Require(Convert.ToHexString(SHA256.HashData(bytes)).Equals(sound.Sha256,StringComparison.OrdinalIgnoreCase));
            using var wave=new MemoryStream(bytes);WaveSound.Validate(wave);
        }
        byte[]? mascot=null;
        if(m.Mascot is {} cat)
        {
            Require(cat.Path is not null&&Regex.IsMatch(cat.Path,"^mascots/[a-z0-9-]{1,48}\\.png$")&&cat.Sha256 is not null&&Regex.IsMatch(cat.Sha256,"^[A-Fa-f0-9]{64}$"));
            Require(cat.FrameWidth is >=64 and <=256&&cat.FrameHeight is >=64 and <=256&&cat.Columns is >=1 and <=16&&cat.Frames is >=2 and <=120&&cat.FrameMilliseconds is >=50 and <=250);
            mascot=ReadEntry(cat.Path!,4_000_000);
            Require(Convert.ToHexString(SHA256.HashData(mascot)).Equals(cat.Sha256,StringComparison.OrdinalIgnoreCase));
            var width=cat.Columns*cat.FrameWidth;var height=((cat.Frames+cat.Columns-1)/cat.Columns)*cat.FrameHeight;
            Require(width<=2048&&height<=2048);
            // Inspect dimensions before handing signed image data to the Windows PNG decoder.
            Require(mascot.Length>=33&&mascot.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})&&mascot.AsSpan(12,4).SequenceEqual("IHDR"u8));
            Require(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(mascot.AsSpan(16))==width&&System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(mascot.AsSpan(20))==height);
        }
        Require(zip.Entries.Count==audio.Count+2+(mascot is null?0:1));
        return new(m,archive.ToArray(),Convert.ToHexString(SHA256.HashData(json)).ToLowerInvariant(),audio,mascot);
    }
}
