using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace AionMeter.App.Services;

/// <summary>Opt-in, current-user game companion. Never starts, stops or modifies the game.</summary>
internal static class GameLifetime
{
    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern uint GetLongPathName(string path, StringBuilder result, int capacity);
    internal static string CanonicalPath(string path)
    {
        var full=Path.GetFullPath(path);
        var buffer=new StringBuilder(32768);
        var length=GetLongPathName(full,buffer,buffer.Capacity);
        return length>0 && length<buffer.Capacity ? buffer.ToString() : full;
    }
    internal static string TaskName(string folder) => "CatScratch-Game-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(folder).TrimEnd('\\').ToUpperInvariant())))[..16];
    private static string Folder => Path.Combine(AppSettings.AppDataDir, "game-start");
    internal static bool NeedsQuietMigration => !File.Exists(Path.Combine(Folder,"watch-quiet.json"));
    internal static bool IsRunning(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable)) return false;
        executable=CanonicalPath(executable);
        var session = Process.GetCurrentProcess().SessionId;
        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable)))
        {
            using (process)
                try
                {
                    if (process.SessionId == session && process.MainModule?.FileName is { } path && string.Equals(CanonicalPath(path), executable, StringComparison.OrdinalIgnoreCase)) return true;
                }
                catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException or NotSupportedException) { }
        }
        return false;
    }

    internal static void Validate(string executable)
    {
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable) || !string.Equals(Path.GetExtension(executable), ".exe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Path.GetFullPath(executable), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Choose the AION 2 game executable first.");
    }

    internal static string Definition(string user, string shell, string script)
    {
        XNamespace ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        XElement E(string name, object? content = null) => new(ns + name, content);
        return new XDocument(new XElement(ns + "Task", new XAttribute("version", "1.2"),
            E("Triggers", new XElement(ns + "LogonTrigger", E("Enabled", "true"), E("UserId", user))),
            E("Principals", new XElement(ns + "Principal", new XAttribute("id", "User"), E("UserId", user), E("LogonType", "InteractiveToken"), E("RunLevel", "HighestAvailable"))),
            E("Settings", new[] { E("MultipleInstancesPolicy", "IgnoreNew"), E("DisallowStartIfOnBatteries", "false"), E("StopIfGoingOnBatteries", "false"), E("ExecutionTimeLimit", "PT0S") }),
            new XElement(ns + "Actions", new XAttribute("Context", "User"), E("Exec", new[] { E("Command", shell), E("Arguments", "\"" + script + "\"") })))).ToString();
    }

    private static async Task Run(params string[] arguments)
    {
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not configure automatic game start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await output;
        if (process.ExitCode != 0) throw new InvalidOperationException((await error).Trim());
        await error;
    }

    public static async Task Configure(bool enabled, string executable)
    {
        var name = TaskName(AppContext.BaseDirectory);
        if (!enabled)
        {
            // Removing the config also stops a watcher if Task Scheduler is unavailable.
            foreach(var file in new[]{"watch.json","watch-quiet.json"})
            { var config=Path.Combine(Folder,file); if(File.Exists(config)) File.Delete(config); }
            try { await Run("/Query", "/TN", name); }
            catch (InvalidOperationException) { return; } // No registration for this installation.
            await Run("/Delete", "/TN", name, "/F");
            return;
        }
        Validate(executable);
        try
        {
            Directory.CreateDirectory(Folder);
            // Removing the legacy config lets existing PowerShell watchers exit without killing child meters.
            var legacy=Path.Combine(Folder,"watch.json");
            if(File.Exists(legacy)) File.Delete(legacy);
            var script = Path.Combine(Folder, "watch-quiet.ps1");
            File.WriteAllText(script, Watcher, new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(Folder, "watch-quiet.json"), JsonSerializer.Serialize(new { Game = executable, Meter = Environment.ProcessPath }), new UTF8Encoding(true));
            var launcher=Path.Combine(Folder,"CatScratch.GameWatcher.exe");
            File.Copy(Path.Combine(AppContext.BaseDirectory,"CatScratch.GameWatcher.exe"),launcher,true);
            using var user = WindowsIdentity.GetCurrent();
            var xml = Path.Combine(Folder, "task.xml");
            File.WriteAllText(xml, Definition(user.User!.Value, launcher, script));
            await Run("/Create", "/TN", name, "/XML", xml, "/F");
            // Existing watchers read the config each poll. Never forcibly stop a task and its child meter.
            await Run("/Run", "/TN", name);
        }
        catch
        {
            // A failed opt-in must not leave a watcher enabled behind an unchecked preference.
            try { await Configure(false, ""); } catch(Exception error) { Log.Error("Game watcher rollback",error); }
            throw;
        }
    }

    // Runs separately so the idle watcher never locks binaries that the updater replaces.
    internal const string Watcher = """
        $ErrorActionPreference = 'Stop'
        Add-Type -TypeDefinition @'
        using System.IO;
        using System.Text;
        using System.Runtime.InteropServices;
        public static class CatScratchGamePath {
            [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
            private static extern uint GetLongPathName(string path, StringBuilder result, int capacity);
            public static string Normalize(string path) {
                var full=Path.GetFullPath(path);
                var buffer=new StringBuilder(32768);
                var length=GetLongPathName(full,buffer,buffer.Capacity);
                return length>0 && length<buffer.Capacity ? buffer.ToString() : full;
            }
        }
        '@
        $configPath = Join-Path $PSScriptRoot 'watch-quiet.json'
        $session = (Get-Process -Id $PID).SessionId
        $hash = [Security.Cryptography.SHA256]::Create()
        try { $key = [BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes([CatScratchGamePath]::Normalize($PSScriptRoot).ToUpperInvariant()))).Replace('-','') } finally { $hash.Dispose() }
        $created = $false
        $mutex = [Threading.Mutex]::new($true,('Local\CatScratch.GameWatcher.' + $key),[ref]$created)
        if (!$created) { $mutex.Dispose(); exit }
        try {
        $seen = $false
        while (Test-Path -LiteralPath $configPath) {
            try {
                $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
                if (!(Test-Path -LiteralPath $config.Meter)) { break }
                $gamePath = [CatScratchGamePath]::Normalize($config.Game)
                $gameName = [IO.Path]::GetFileNameWithoutExtension($gamePath)
                $running = @(Get-Process -Name $gameName -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $session -and $_.Path -and [CatScratchGamePath]::Normalize($_.Path) -eq $gamePath }).Count -gt 0
                if ($running -and !$seen) {
                    $seen = $true
                    $meterName = [IO.Path]::GetFileNameWithoutExtension($config.Meter)
                    $meter = @(Get-Process -Name $meterName -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $session }).Count -gt 0
                    if (!$meter) { Start-Process -FilePath $config.Meter -ArgumentList '--game-session' -WorkingDirectory ([IO.Path]::GetDirectoryName($config.Meter)) -WindowStyle Normal }
                }
                if (!$running) { $seen = $false }
            } catch {
                try { [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'watch-error.txt'), $_.Exception.ToString()) } catch { }
            }
            Start-Sleep -Seconds 2
        }
        } finally { $mutex.ReleaseMutex(); $mutex.Dispose() }
        """;
}

/// <summary>Require a confirmed game session and tolerate brief restarts or launcher hand-offs.</summary>
internal sealed class GameExitGate
{
    private bool _seen;
    private DateTimeOffset? _missing;
    public bool ShouldClose(bool enabled, bool running, DateTimeOffset now)
    {
        if (!enabled) { _seen = false; _missing = null; return false; }
        if (running) { _seen = true; _missing = null; return false; }
        if (!_seen) return false;
        _missing ??= now;
        return now - _missing >= TimeSpan.FromSeconds(10);
    }
}
