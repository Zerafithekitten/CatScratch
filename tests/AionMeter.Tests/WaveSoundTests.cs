using AionMeter.Core.Audio;

namespace AionMeter.Tests;

public sealed class WaveSoundTests
{
    private static MemoryStream Wave(int samples, uint byteRate = 16000)
    {
        var stream = new MemoryStream(); using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true);
        writer.Write("RIFF"u8); writer.Write(36 + samples * 2); writer.Write("WAVEfmt "u8); writer.Write(16);
        writer.Write((ushort)1); writer.Write((ushort)1); writer.Write(8000u); writer.Write(byteRate);
        writer.Write((ushort)2); writer.Write((ushort)16); writer.Write("data"u8); writer.Write(samples * 2); writer.Write(new byte[samples * 2]);
        stream.Position = 0; return stream;
    }
    [Theory] [InlineData(1)] [InlineData(80000)]
    public void Accepts_up_to_exactly_ten_seconds(int samples)
    { using var stream = Wave(samples); Assert.Equal(TimeSpan.FromSeconds(samples / 8000d), WaveSound.Validate(stream)); }
    [Theory] [InlineData(80001,16000u)] [InlineData(88000,160000u)]
    public void Rejects_long_audio_and_falsified_byte_rate(int samples, uint byteRate)
    { using var stream = Wave(samples,byteRate); Assert.Throws<InvalidDataException>(()=>WaveSound.Validate(stream)); }
    [Fact] public void Rejects_truncated_audio()
    { using var stream = Wave(100); stream.SetLength(stream.Length-2); Assert.Throws<InvalidDataException>(()=>WaveSound.Validate(stream)); }
    [Fact] public void Skips_metadata_chunks_and_measures_samples_instead_of_fact_duration()
    {
        using var original=Wave(8000); var bytes=original.ToArray();
        using var stream=new MemoryStream(); using var writer=new BinaryWriter(stream,System.Text.Encoding.ASCII,true);
        writer.Write(bytes.AsSpan(0,36)); writer.Write("JUNK"u8); writer.Write(3); writer.Write(new byte[]{1,2,3,0});
        writer.Write("fact"u8); writer.Write(4); writer.Write(1); writer.Write(bytes.AsSpan(36));
        stream.Position=4; writer.Write((int)stream.Length-8);
        Assert.Equal(TimeSpan.FromSeconds(1),WaveSound.Validate(stream));
    }
    [Fact] public void Rejects_compressed_wave_instead_of_trusting_declared_duration()
    {
        using var stream=Wave(8000); stream.Position=20; stream.WriteByte(2);
        Assert.Throws<InvalidDataException>(()=>WaveSound.Validate(stream));
    }
}
