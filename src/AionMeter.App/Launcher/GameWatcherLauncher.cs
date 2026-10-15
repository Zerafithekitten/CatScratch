using System;
using System.Diagnostics;
using System.IO;

// A tiny GUI-subsystem launcher using the Windows .NET Framework. It exits immediately and never locks meter files.
internal static class GameWatcherLauncher
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 1 || !File.Exists(args[0]) || !string.Equals(Path.GetExtension(args[0]), ".ps1", StringComparison.OrdinalIgnoreCase)) return 1;
        try
        {
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"));
            start.Arguments = "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File \"" + Path.GetFullPath(args[0]) + "\"";
            start.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(args[0]));
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.WindowStyle = ProcessWindowStyle.Hidden;
            using (var process = Process.Start(start)) return process == null ? 1 : 0;
        }
        catch { return 1; }
    }
}
