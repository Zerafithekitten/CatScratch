using System.IO;
using System.Text.Json;

namespace AionMeter.App.Services;

/// <summary>Atomic preference writes with a last readable backup. No combat recordings are stored here.</summary>
internal static class JsonSettingsFile
{
    private static readonly object Gate = new();

    internal static T? Read<T>(string path, JsonSerializerOptions? options = null) where T : class
    {
        lock (Gate)
        {
            foreach (var candidate in new[] { path, path + ".bak" })
                try
                {
                    if (File.Exists(candidate) && JsonSerializer.Deserialize<T>(File.ReadAllText(candidate), options) is { } value)
                        return value;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
                { Log.Error("Preference file unreadable: " + Path.GetFileName(candidate), error); }
            return null;
        }
    }

    internal static void Write<T>(string path, T value, JsonSerializerOptions? options = null)
    {
        var text = JsonSerializer.Serialize(value, options);
        lock (Gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    using var writer = new StreamWriter(stream, leaveOpen: true);
                    writer.Write(text); writer.Flush(); stream.Flush(flushToDisk: true);
                }
                if (File.Exists(path))
                {
                    // A damaged primary must never replace the last readable backup.
                    bool readable;
                    try { readable = JsonSerializer.Deserialize<T>(File.ReadAllText(path), options) is not null; }
                    catch (JsonException) { readable = false; }
                    File.Replace(temporary, path, readable ? path + ".bak" : null);
                }
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
