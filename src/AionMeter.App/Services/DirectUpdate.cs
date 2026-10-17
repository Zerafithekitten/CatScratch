using System.Diagnostics;
using System.IO;
using System.Windows;
using AionMeter.Core.Updates;

namespace AionMeter.App.Services;

internal static class DirectUpdate
{
    public static string Root => RootFor(AppContext.BaseDirectory);
    internal static string RootFor(string installation) => Path.Combine(Path.GetFullPath(installation), "userdata", "temp", "updates");

    public static void Launch(string archive, Version version, string? checkResult = null, ReleaseVersion? identity = null)
        => LaunchPrepared(Prepare(archive, version, identity), version, checkResult, identity);

    internal static string Prepare(string archive, Version version, ReleaseVersion? identity = null)
    {
        var stage = Path.Combine(Root, "stage-" + Guid.NewGuid().ToString("N"));
        var payload = Path.Combine(stage, "payload");
        try
        {
            UpdatePackage.Extract(archive, payload);
            ValidatePayload(payload, version, identity);
            return stage;
        }
        catch { if (Directory.Exists(stage)) UpdateCleanup.RemoveStage(Root, stage); throw; }
    }

    private static void ValidatePayload(string payload, Version version, ReleaseVersion? identity)
    {
        var exe = Path.Combine(payload, "CatScratch.exe");
        var info = FileVersionInfo.GetVersionInfo(exe);
        if (!UpdateFeed.IsProductName(info.ProductName, AppInfo.Name) || UpdateFeed.ParseVersion(info.ProductVersion) != version)
            throw new InvalidDataException("The update's product or version does not match its release.");
        if (identity is not null && ReleaseVersion.Parse(info.ProductVersion) != identity)
            throw new InvalidDataException("The update's beta revision does not match its release.");
    }

    internal static void LaunchPrepared(string stage, Version version, string? checkResult = null, ReleaseVersion? identity = null)
    {
        if (!UpdateCleanup.IsStage(Root, stage)) throw new InvalidDataException("Invalid prepared update path.");
        var payload = Path.Combine(stage, "payload");
        ValidatePayload(payload, version, identity);
        var exe = Path.Combine(payload, "CatScratch.exe");
        var start = new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = payload };
        foreach (var arg in new[] { "--apply-update", AppContext.BaseDirectory, "--parent", Environment.ProcessId.ToString() })
            start.ArgumentList.Add(arg);
        if (checkResult is not null) { start.ArgumentList.Add("--update-check"); start.ArgumentList.Add(checkResult); }
        using var helper = Process.Start(start) ?? throw new InvalidOperationException("Could not start the updater.");
        if (checkResult is not null)
        {
            File.WriteAllText(checkResult + ".helperpid", helper.Id.ToString());
            File.WriteAllText(checkResult + ".stage", stage);
        }
    }

    public static void Run(string target, int parentId, string? checkResult = null)
    {
        var stage = Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar))!.FullName;

        target = Path.GetFullPath(target);
        var boundary = RootFor(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        // Compatibility for the one migration update launched by an older installed version.
        var legacyBoundary = Path.Combine(Path.GetTempPath(), "CatScratch-update") + Path.DirectorySeparatorChar;
        var targetExe = UpdatePackage.Resolve(target, "CatScratch.exe");
        if ((!stage.StartsWith(boundary, StringComparison.OrdinalIgnoreCase) &&
             !stage.StartsWith(legacyBoundary, StringComparison.OrdinalIgnoreCase)) ||
            !string.Equals(Path.GetDirectoryName(stage) + Path.DirectorySeparatorChar,
                stage.StartsWith(boundary, StringComparison.OrdinalIgnoreCase) ? boundary : legacyBoundary, StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(stage).Length != 38 || !Path.GetFileName(stage).StartsWith("stage-", StringComparison.Ordinal) ||
            target.StartsWith(boundary, StringComparison.OrdinalIgnoreCase) ||
            !UpdateFeed.IsProductName(FileVersionInfo.GetVersionInfo(targetExe).ProductName, AppInfo.Name))
            throw new InvalidDataException("Invalid update destination.");
        // Only wait for the requesting meter; never terminate the game or another process.
        try
        {
            using var parent = Process.GetProcessById(parentId);
            if (!string.Equals(parent.MainModule?.FileName, targetExe, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Update requester does not match this installation.");
            if (!parent.WaitForExit(30000)) throw new IOException("The meter has not closed. The update was cancelled.");
        }
        catch (ArgumentException) { } // The meter has already finished closing.
        var backup = Path.Combine(stage, "backup");
        try
        {
            UpdatePackage.Apply(AppContext.BaseDirectory, target, backup);
            if(checkResult is null)ShortcutIcons.Refresh(target);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(stage, "failure.txt"), error.ToString());
            System.Windows.MessageBox.Show("The update could not be applied. Previous files were restored where possible.\n\n" +
                "Details and backups: " + stage, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        var restart = new ProcessStartInfo(targetExe) { UseShellExecute = false, WorkingDirectory = target };
        if (!File.Exists(Path.Combine(stage, "failure.txt")))
        {
            restart.ArgumentList.Add("--updated");
            restart.ArgumentList.Add("--update-ready"); restart.ArgumentList.Add(stage);
        }
        if (checkResult is not null)
        {
            restart.ArgumentList.Add("--check-shutdown");
            restart.ArgumentList.Add(checkResult);
        }
        using var restarted = Process.Start(restart) ?? throw new IOException("Could not restart CatScratch.");
        if (checkResult is not null && (!restarted.WaitForExit(20000) || restarted.ExitCode != 0))
            throw new IOException("Updated program verification failed.");
        if (checkResult is not null && !File.Exists(Path.Combine(stage, "failure.txt")))
            File.WriteAllText(Path.Combine(stage, "started.txt"), "Verified startup completed");
        if (checkResult is null)
        {
            var deadline = Stopwatch.StartNew();
            while (!File.Exists(Path.Combine(stage,"started.txt")) && !restarted.HasExited && deadline.Elapsed < TimeSpan.FromSeconds(30)) Thread.Sleep(100);
        }
        if (!File.Exists(Path.Combine(stage,"failure.txt")) && (checkResult is not null || File.Exists(Path.Combine(stage,"started.txt"))))
        {
            // Run from the installed copy: the staging helper's own executable is locked until it exits.
            var cleanup = new ProcessStartInfo(targetExe) { UseShellExecute = false, WorkingDirectory = target, CreateNoWindow = true };
            foreach (var arg in new[] { "--clean-update", stage, "--parent", Environment.ProcessId.ToString() }) cleanup.ArgumentList.Add(arg);
            using var cleaner = Process.Start(cleanup);
            if (checkResult is not null && cleaner is not null) File.WriteAllText(checkResult + ".cleanerpid", cleaner.Id.ToString());
        }
    }

    internal static void MarkReady(string stage)
    {
        if (!UpdateCleanup.IsStage(Root,stage) || File.Exists(Path.Combine(stage, "failure.txt"))) return;
        var staged = FileVersionInfo.GetVersionInfo(Path.Combine(stage, "payload", "CatScratch.exe"));
        var installed = FileVersionInfo.GetVersionInfo(Path.Combine(AppContext.BaseDirectory, "CatScratch.exe"));
        if (ReleaseVersion.Parse(installed.ProductVersion) is not { } expected || ReleaseVersion.Parse(staged.ProductVersion) != expected) return;
        File.WriteAllText(Path.Combine(stage,"started.txt"),"Startup completed");
    }

    internal static void Clean(string stage, int parentId)
    {
        if (!UpdateCleanup.IsStage(Root,stage)) throw new InvalidDataException("Invalid update cleanup path.");
        try
        {
            if (parentId > 0)
            {
            using var helper = Process.GetProcessById(parentId);
            if (!string.Equals(helper.MainModule?.FileName,Path.Combine(stage,"payload","CatScratch.exe"),StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Cleanup requester does not match this update.");
            if (!helper.WaitForExit(30000)) return;
            }
        }
        catch (ArgumentException) { }
        if (File.Exists(Path.Combine(stage,"failure.txt")) || !File.Exists(Path.Combine(stage,"started.txt"))) return;
        var stageCreated = Directory.GetCreationTimeUtc(stage);
        for (var attempt = 0; ; attempt++)
        {
            try { UpdateCleanup.RemoveStage(Root,stage); break; }
            catch (IOException) when (attempt < 50) { Thread.Sleep(200); }
        }
        UpdateCleanup.PruneOlderStages(Root,stageCreated);
        // Only the recognized package created by our downloader, never arbitrary files in the install folder.
        UpdateCleanup.RemoveDownload(Root,"CatScratch-win-x64.zip",stageCreated);
    }

    internal static void CleanLegacyAfterStartup()
    {
        // Older update helpers only pass --updated. Once this app has initialized, locate the matching
        // staging package and let the installed copy remove it after the old helper unlocks its files.
        if (!Directory.Exists(Root)) return;
        foreach (var stage in Directory.EnumerateDirectories(Root,"stage-*"))
        {
            if (!UpdateCleanup.IsStage(Root,stage) || File.Exists(Path.Combine(stage,"failure.txt"))) continue;
            var stagedExe = Path.Combine(stage,"payload","CatScratch.exe");
            if (!File.Exists(stagedExe) || UpdateFeed.ParseVersion(FileVersionInfo.GetVersionInfo(stagedExe).ProductVersion) !=
                UpdateFeed.Normalize(typeof(App).Assembly.GetName().Version!)) continue;
            MarkReady(stage);
            if (!File.Exists(Path.Combine(stage, "started.txt"))) continue;
            var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"CatScratch.exe")) { UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=AppContext.BaseDirectory };
            foreach (var arg in new[] { "--clean-update", stage, "--parent", "0" }) start.ArgumentList.Add(arg);
            using var cleaner = Process.Start(start);
            break;
        }
    }
}
