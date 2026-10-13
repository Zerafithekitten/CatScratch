using System.Text;

namespace AionMeter.Core.Audio;

/// <summary>Validates the actual PCM sample count, never a filename or untrusted duration field.</summary>
public static class WaveSound
{
    public const int MaximumSeconds = 10;
    public static TimeSpan Validate(Stream stream)
    {
        static InvalidDataException Invalid() => new("Choose a valid PCM or float WAV file, up to 10 seconds and 10 MB.");
        if (!stream.CanSeek || stream.Length < 44 || stream.Length > 10_000_000) throw Invalid();
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        stream.Position = 0;
        try
        {
            if (reader.ReadUInt32() != 0x46464952) throw Invalid();
            var end = (long)reader.ReadUInt32() + 8;
            if (end != stream.Length || reader.ReadUInt32() != 0x45564157) throw Invalid();
            uint rate = 0; ushort alignment = 0; long samplesBytes = 0; bool hasFormat = false;
            while (stream.Position < end)
            {
                if (end - stream.Position < 8) throw Invalid();
                var id = reader.ReadUInt32(); var size = reader.ReadUInt32(); var next = stream.Position + size;
                if (next + (size & 1) > end) throw Invalid();
                if (id == 0x20746d66)
                {
                    if (hasFormat || size < 16) throw Invalid();
                    var format = reader.ReadUInt16(); var channels = reader.ReadUInt16(); rate = reader.ReadUInt32();
                    var byteRate = reader.ReadUInt32(); alignment = reader.ReadUInt16(); var bits = reader.ReadUInt16();
                    if (format == 0xfffe)
                    {
                        if (size < 40 || reader.ReadUInt16() < 22) throw Invalid();
                        reader.ReadUInt16(); reader.ReadUInt32();
                        var subtype = new Guid(reader.ReadBytes(16));
                        format = subtype == new Guid("00000001-0000-0010-8000-00aa00389b71") ? (ushort)1 :
                            subtype == new Guid("00000003-0000-0010-8000-00aa00389b71") ? (ushort)3 : (ushort)0;
                    }
                    if (format is not (1 or 3) || channels is 0 or > 32 || rate is 0 or > 384000 ||
                        bits is not (8 or 16 or 24 or 32 or 64) || format == 3 && bits is not (32 or 64) ||
                        alignment != channels * (bits / 8) || byteRate != (long)rate * alignment) throw Invalid();
                    hasFormat = true;
                }
                else if (id == 0x61746164) samplesBytes += size;
                stream.Position = next + (size & 1);
            }
            if (!hasFormat || samplesBytes <= 0 || samplesBytes % alignment != 0 || samplesBytes > (long)rate * alignment * MaximumSeconds) throw Invalid();
            return TimeSpan.FromSeconds(samplesBytes / (double)alignment / rate);
        }
        catch (EndOfStreamException) { throw Invalid(); }
    }
}
