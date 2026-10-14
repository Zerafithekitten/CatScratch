namespace AionMeter.Core.Updates;

/// <summary>Removes only update-owned temporary files; never traverses links or installation userdata.</summary>
public static class UpdateCleanup
{
    public static bool IsStage(string root, string stage)
    {
        var path = Path.GetFullPath(stage).TrimEnd(Path.DirectorySeparatorChar);
        var name = Path.GetFileName(path);
        return string.Equals(Path.GetDirectoryName(path),Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)
            && name.StartsWith("stage-",StringComparison.Ordinal) && name.Length == 38 && Guid.TryParseExact(name[6..],"N",out _);
    }

    public static void RemoveStage(string root, string stage)
    {
        if (!IsStage(root,stage)) throw new InvalidDataException("Cleanup path is outside the update directory.");
        if (!Directory.Exists(stage) || File.Exists(Path.Combine(stage,"failure.txt"))) return;
        // A failed startup may leave no failure marker. Keep the previous program until startup is acknowledged.
        if (Directory.Exists(Path.Combine(stage, "backup")) && !File.Exists(Path.Combine(stage, "started.txt"))) return;
        CheckLinks(stage);
        // Validate every descendant before deleting anything. Enumeration does not enter linked directories.
        var entries = new Stack<string>(); entries.Push(stage);
        while (entries.TryPop(out var dir))
            foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
            {
                CheckLinks(entry);
                if (Directory.Exists(entry)) entries.Push(entry);
            }
        Directory.Delete(stage,true);
    }

    public static void RemoveDownload(string root, string name, DateTime? downloadedBefore = null)
    {
        if (name != "CatScratch-win-x64.zip") throw new InvalidDataException("Unrecognized update download.");
        var path = Path.Combine(root,name);
        if (!File.Exists(path)) return;
        if (downloadedBefore is { } cutoff && File.GetLastWriteTimeUtc(path) > cutoff) return;
        CheckLinks(path); File.Delete(path);
    }

    public static void PruneOlderStages(string root, DateTime before)
    {
        if (!Directory.Exists(root)) return;
        foreach (var stage in Directory.EnumerateDirectories(root,"stage-*"))
        {
            if (!IsStage(root,stage) || Directory.GetCreationTimeUtc(stage) >= before) continue;
            try { RemoveStage(root,stage); }
            catch (IOException) { } // A locked or linked stage is retained; other safe stages can still be cleaned.
            catch (InvalidDataException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void CheckLinks(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Cleanup path contains a link.");
    }
}
