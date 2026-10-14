using AionMeter.Core.Capture;

namespace AionMeter.Tests;

public class RecentPacketBufferTests
{
    [Fact]
    public void BoundariesAndDisableReleaseHistory()
    {
        long now = 0;
        var buffer = new RecentPacketBuffer(200, 60_000, () => now);
        buffer.Add(1, 0, 1, new byte[10]); Assert.Equal(0, buffer.Count);
        buffer.Enabled = true;
        buffer.Add(1, 0, 1, new byte[36]); buffer.Add(2, 0, 1, new byte[36]);
        Assert.Equal(200, buffer.Bytes);
        buffer.Add(3, 0, 1, new byte[36]); Assert.Equal(2, buffer.Count);
        buffer.Add(4, 0, 1, new byte[201]); Assert.Equal(2, buffer.Count);
        now = 60_000; Assert.Equal(0, buffer.Bytes);
        buffer.Add(5, 0, 1, new byte[36]); buffer.Enabled = false;
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void ExportPreservesTimestampAndOwnsPacketCopy()
    {
        var buffer = new RecentPacketBuffer { Enabled = true };
        byte[] bytes = [1, 2, 3]; buffer.Add(123, 456, 1, bytes); bytes[0] = 9;
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pcap");
        try
        {
            buffer.Save(path);
            using var reader = new BinaryReader(File.OpenRead(path));
            Assert.Equal(0xa1b2c3d4u, reader.ReadUInt32());
            reader.BaseStream.Position = 20; Assert.Equal(1u, reader.ReadUInt32());
            Assert.Equal(123u, reader.ReadUInt32()); Assert.Equal(456u, reader.ReadUInt32());
            Assert.Equal(3u, reader.ReadUInt32()); Assert.Equal(3u, reader.ReadUInt32());
            Assert.Equal(new byte[] { 1, 2, 3 }, reader.ReadBytes(3));
            Assert.Throws<IOException>(() => buffer.Save(path));
            buffer.Add(124, 0, 101, [4]); Assert.Equal(1, buffer.Count);
        }
        finally { File.Delete(path); }
    }
}
