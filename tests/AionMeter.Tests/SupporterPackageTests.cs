using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AionMeter.Core.Supporter;
namespace AionMeter.Tests;
public class SupporterPackageTests
{
    private static byte[] Wave(){using var m=new MemoryStream();using var w=new BinaryWriter(m);w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+200);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((ushort)1);w.Write((ushort)1);w.Write(8000);w.Write(16000);w.Write((ushort)2);w.Write((ushort)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(200);w.Write(new byte[200]);return m.ToArray();}
    private static SupporterManifest Manifest(byte[] wave)=>new(1,"test-pack","Test Pack","1.0.0",true,[new("Moonlight","#97CBE8","#20242A","#EDF2F7")],[new("Test tone","sounds/test.wav",Convert.ToHexString(SHA256.HashData(wave)))]);
    private static byte[] Zip(ECDsa key,SupporterManifest manifest,byte[] wave,Action<ZipArchive>? extra=null,bool tamper=false)
    {
        using var output=new MemoryStream();using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
        {
            void Entry(string path,byte[] data){using var s=zip.CreateEntry(path).Open();s.Write(data);}
            var json=JsonSerializer.SerializeToUtf8Bytes(manifest);var signature=key.SignData(json,HashAlgorithmName.SHA256);if(tamper)json[0]=(byte)' ';
            Entry("manifest.json",json);Entry("manifest.sig",signature);if(manifest.Sounds.Length>0)Entry(manifest.Sounds[0].Path,wave);extra?.Invoke(zip);
        }return output.ToArray();
    }
    [Fact] public void SignedDataPackageLoadsThemesAudioAndPawWithoutAnyCode()
    {using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var wave=Wave();var pack=SupporterPackage.Read(Zip(key,Manifest(wave),wave),key.ExportSubjectPublicKeyInfoPem());Assert.True(pack.Manifest.SupporterPaw);Assert.Single(pack.Audio);Assert.Equal("Moonlight",pack.Manifest.Themes[0].Name);}
    [Fact] public void ChangedManifestAndUnknownSigningKeyAreRejected()
    {using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);using var other=ECDsa.Create(ECCurve.NamedCurves.nistP256);var w=Wave();Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,Manifest(w),w,tamper:true),key.ExportSubjectPublicKeyInfoPem()));Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,Manifest(w),w),other.ExportSubjectPublicKeyInfoPem()));}
    [Fact] public void ChangedAudioFailsEvenWithAnAuthenticManifest()
    {using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var w=Wave();var manifest=Manifest(w);w[^1]=1;Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,manifest,w),key.ExportSubjectPublicKeyInfoPem()));}
    [Theory][InlineData("../escape.wav")][InlineData("/absolute.wav")][InlineData("sounds/test.exe")][InlineData("sounds\\test.wav")]
    public void ArchivePathsAndExecutableAssetsAreRejected(string path)
    {using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var w=Wave();var m=Manifest(w) with{Sounds=[new("Test",path,Convert.ToHexString(SHA256.HashData(w)))]};Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,m,w),key.ExportSubjectPublicKeyInfoPem()));}
    [Fact] public void UnlistedAndDuplicateArchiveEntriesAreRejected()
    {using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var w=Wave();foreach(var name in new[]{"plugin.dll","sounds/test.wav"})Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,Manifest(w),w,z=>z.CreateEntry(name)),key.ExportSubjectPublicKeyInfoPem()));}
    [Fact] public void InvalidThemeColorsAndUnsupportedFormatAreRejected()
    {using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var w=Wave();foreach(var m in new[]{Manifest(w) with{Format=2},Manifest(w) with{Themes=[new("Bad","not-a-color","#20242A","#EDF2F7")]}})Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,m,w),key.ExportSubjectPublicKeyInfoPem()));}
    [Fact] public void OversizeArchivesAndMalformedAudioAreRejected()
    {using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(new byte[8_000_001],key.ExportSubjectPublicKeyInfoPem()));var bad=new byte[100];Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,Manifest(bad),bad),key.ExportSubjectPublicKeyInfoPem()));}
    private static byte[] Png(int width,int height)
    {
        using var output=new MemoryStream();output.Write(new byte[]{137,80,78,71,13,10,26,10});
        void Chunk(string name,byte[] bytes)
        {
            var length=new byte[4];System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length,bytes.Length);output.Write(length);
            var kind=Encoding.ASCII.GetBytes(name);output.Write(kind);output.Write(bytes);
            uint crc=0xffffffff;
            foreach(var b in kind.Concat(bytes)){crc^=b;for(var bit=0;bit<8;bit++)crc=(crc&1)!=0?(crc>>1)^0xedb88320:crc>>1;}
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(length,crc^0xffffffff);output.Write(length);
        }
        var header=new byte[13];System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header,width);System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4),height);header[8]=8;header[9]=6;Chunk("IHDR",header);
        using var compressed=new MemoryStream();using(var z=new System.IO.Compression.ZLibStream(compressed,CompressionLevel.Fastest,true))z.Write(new byte[(width*4+1)*height]);
        Chunk("IDAT",compressed.ToArray());Chunk("IEND",[]);return output.ToArray();
    }
    [Fact] public void SeparateSignedMascotPackageNeedsNoThemesSoundsOrPaw()
    {
        using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var png=Png(128,64);var wave=Wave();
        var mascot=new SupporterMascot("mascots/cat.png",Convert.ToHexString(SHA256.HashData(png)),64,64,2,2,83);
        var manifest=Manifest(wave) with{SupporterPaw=false,Themes=[],Sounds=[],Mascot=mascot};
        var pack=SupporterPackage.Read(Zip(key,manifest,wave,z=>{using var entry=z.CreateEntry(mascot.Path).Open();entry.Write(png);}),key.ExportSubjectPublicKeyInfoPem());
        Assert.Empty(pack.Audio);Assert.False(pack.Manifest.SupporterPaw);Assert.Equal(png,pack.MascotImage);Assert.Equal(2,pack.Manifest.Mascot!.Frames);
    }
    [Fact] public void MascotMetadataPathsAndImageHashAreValidatedBeforeDecoding()
    {
        using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var png=Png(128,64);var wave=Wave();
        var mascot=new SupporterMascot("mascots/cat.png",Convert.ToHexString(SHA256.HashData(png)),64,64,2,2,83);
        foreach(var bad in new[]{mascot with{Path="../cat.png"},mascot with{Frames=121},mascot with{FrameMilliseconds=1},mascot with{FrameWidth=256},mascot with{Columns=17},mascot with{Sha256=new string('0',64)}})
        {
            var m=Manifest(wave) with{Mascot=bad};
            Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,m,wave,z=>{using var e=z.CreateEntry(bad.Path).Open();e.Write(png);}),key.ExportSubjectPublicKeyInfoPem()));
        }
    }
    [Fact] public void MascotWithoutDeclaredImageOrWithWrongSignatureIsRejected()
    {
        using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var wave=Wave();var png=Png(128,64);png[0]=0;
        var cat=new SupporterMascot("mascots/cat.png",Convert.ToHexString(SHA256.HashData(png)),64,64,2,2,83);
        var m=Manifest(wave) with{Mascot=cat};
        Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,m,wave),key.ExportSubjectPublicKeyInfoPem()));
        Assert.Throws<InvalidDataException>(()=>SupporterPackage.Read(Zip(key,m,wave,z=>{using var e=z.CreateEntry(cat.Path).Open();e.Write(png);}),key.ExportSubjectPublicKeyInfoPem()));
    }
}
