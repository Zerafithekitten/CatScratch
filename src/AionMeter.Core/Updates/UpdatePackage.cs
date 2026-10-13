using System.IO.Compression;
using System.Text.Json;

namespace AionMeter.Core.Updates;

/// <summary>Updates only files supplied by the package; userdata and installer files are never eligible.</summary>
public static class UpdatePackage
{
    private const string Manifest = ".catscratch-files.json";

    public static string Resolve(string root, string relative)
    {
        relative = relative.Replace('\\', '/');
        var parts = relative.Split('/');
        if (parts.Any(p => p.Length == 0 || p is "." or ".." || p.Any(c => c < 32 || "<>:\"|?*".Contains(c)) || p.EndsWith(' ') || p.EndsWith('.')) ||
            parts[0].Equals("userdata", StringComparison.OrdinalIgnoreCase) ||
            parts[0].StartsWith("unins", StringComparison.OrdinalIgnoreCase) || Path.IsPathRooted(relative))
            throw new InvalidDataException("Unsafe update path: " + relative);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var boundary = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(boundary, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Update path escapes its folder.");
        for (var check = path; check is not null; check = Path.GetDirectoryName(check))
        {
            if ((File.Exists(check) || Directory.Exists(check)) && (File.GetAttributes(check) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Update path contains a link.");
        }
        return path;
    }

    public static void Extract(string archive, string destination)
    {
        Directory.CreateDirectory(destination);
        using var zip = ZipFile.OpenRead(archive);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var entry in zip.Entries)
        {
            var relative = entry.FullName.Replace('\\', '/').TrimEnd('/');
            if (relative.Equals(Manifest, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Reserved package entry.");
            var path = Resolve(destination, relative);
            if (!names.Add(relative)) throw new InvalidDataException("Duplicate package entry.");
            total += entry.Length;
            if (total > 1_000_000_000 || zip.Entries.Count > 10000) throw new InvalidDataException("Update package is too large.");
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\')) { Directory.CreateDirectory(path); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            entry.ExtractToFile(path, overwrite: false);
        }
        if (!File.Exists(Path.Combine(destination, "CatScratch.exe"))) throw new InvalidDataException("Package has no CatScratch executable.");
    }

    public static void Apply(string source, string target, string backup)
    {
        var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(source, p)).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        // Validate everything before touching any installed file.
        foreach (var name in files) { Resolve(source, name); Resolve(target, name); Resolve(backup, name); }
        var manifestPath = Resolve(target, Manifest);
        var previous = File.Exists(manifestPath) ? JsonSerializer.Deserialize<string[]>(File.ReadAllText(manifestPath)) ?? [] : [];
        var obsolete = previous.Except(files, StringComparer.OrdinalIgnoreCase).Where(p => p != Manifest).ToArray();
        foreach (var name in obsolete) { Resolve(target, name); Resolve(backup, name); }
        var touched = new List<(string Name, bool Existed)>();
        try
        {
            foreach (var name in files.Concat(obsolete).Append(Manifest).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var path = Resolve(target, name);
                var existed = File.Exists(path);
                if (existed)
                {
                    var saved = Resolve(backup, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(saved)!);
                    File.Copy(path, saved, false);
                }
                touched.Add((name, existed));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (name == Manifest) File.WriteAllText(path, JsonSerializer.Serialize(files));
                else if (files.Contains(name, StringComparer.OrdinalIgnoreCase)) File.Copy(Resolve(source, name), path, true);
                else File.Delete(path);
            }
        }
        catch (Exception failure)
        {
            var errors = new List<Exception> { failure };
            foreach (var (name, existed) in touched.AsEnumerable().Reverse())
            {
                try
                {
                    var path = Resolve(target, name);
                    if (existed) File.Copy(Resolve(backup, name), path, true); else if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception error) { errors.Add(error); }
            }
            if (errors.Count > 1) throw new AggregateException("Update and restoration failed; backups are retained.", errors);
            throw;
        }
    }
}
