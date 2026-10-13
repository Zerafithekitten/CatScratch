using System.IO.Compression;
using AionMeter.Core.Updates;

namespace AionMeter.Tests;

public sealed class UpdatePackageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "catscratch-test-" + Guid.NewGuid().ToString("N"));
    private string Folder(string name) { var p = Path.Combine(_root, name); Directory.CreateDirectory(p); return p; }
    private static void Write(string folder, string file, string value)
    {
        var p = Path.Combine(folder, file); Directory.CreateDirectory(Path.GetDirectoryName(p)!); File.WriteAllText(p, value);
    }

    [Fact]
    public void Replaces_program_preserves_userdata_and_removes_only_previously_supplied_files()
    {
        var source = Folder("source"); var target = Folder("target");
        Write(target, "CatScratch.exe", "old"); Write(target, "userdata/settings.json", "keep");
        Write(target, "unins000.exe", "keep"); Write(target, "custom.txt", "keep");
        Write(source, "CatScratch.exe", "new"); Write(source, "obsolete.dll", "first");
        UpdatePackage.Apply(source, target, Folder("backup1"));
        File.Delete(Path.Combine(source, "obsolete.dll"));
        UpdatePackage.Apply(source, target, Folder("backup2"));
        Assert.Equal("new", File.ReadAllText(Path.Combine(target, "CatScratch.exe")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(target, "userdata/settings.json")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(target, "unins000.exe")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(target, "custom.txt")));
        Assert.False(File.Exists(Path.Combine(target, "obsolete.dll")));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("userdata/settings.json")]
    [InlineData("unins000.exe")]
    [InlineData("C:/game/file")]
    [InlineData("data/../../game")]
    public void Unsafe_archive_paths_are_rejected(string name)
    {
        var archive = Path.Combine(Folder("zip"), "test.zip");
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
        using (var writer = new StreamWriter(zip.CreateEntry(name).Open())) writer.Write("bad");
        Assert.Throws<InvalidDataException>(() => UpdatePackage.Extract(archive, Folder("payload")));
    }

    [Fact]
    public void Failed_copy_restores_already_replaced_files_and_leaves_userdata()
    {
        var source = Folder("source"); var target = Folder("target"); var backup = Folder("backup");
        Write(source, "a-first.dll", "new"); Write(source, "z-blocked.dll", "new");
        Write(target, "a-first.dll", "old"); Write(target, "userdata/settings.json", "keep");
        // A directory where a file belongs forces failure after an earlier file was replaced.
        Directory.CreateDirectory(Path.Combine(target, "z-blocked.dll"));
        Assert.ThrowsAny<Exception>(() => UpdatePackage.Apply(source, target, backup));
        Assert.Equal("old", File.ReadAllText(Path.Combine(target, "a-first.dll")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(target, "userdata/settings.json")));
    }

    [Fact]
    public void Tampered_previous_manifest_cannot_delete_userdata()
    {
        var source = Folder("source"); var target = Folder("target");
        Write(source, "CatScratch.exe", "new"); Write(target, "CatScratch.exe", "old");
        Write(target, ".catscratch-files.json", "[\"userdata/settings.json\"]");
        Assert.Throws<InvalidDataException>(() => UpdatePackage.Apply(source, target, Folder("backup")));
        Assert.Equal("old", File.ReadAllText(Path.Combine(target, "CatScratch.exe")));
    }

    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
