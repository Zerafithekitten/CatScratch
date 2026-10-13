using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace AionMeter.App.Services;

internal static class AdministratorStartup
{
    internal static ProcessStartInfo StartInfo(string executable, IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(executable) { UseShellExecute = true, Verb = "runas", WorkingDirectory = AppContext.BaseDirectory };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return start;
    }
    // Called before single-instance handles, settings, windows or capture are initialized.
    public static bool Ensure(string[] arguments)
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return true;
        try
        {
            using var elevated = Process.Start(StartInfo(Environment.ProcessPath ?? throw new InvalidOperationException("CatScratch executable not found."), arguments));
            if (elevated is null) throw new InvalidOperationException("Could not start CatScratch as administrator.");
        }
        catch (Win32Exception error) when (error.NativeErrorCode == 1223) { } // UAC cancelled: do not run the meter without admin rights.
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        { System.Windows.MessageBox.Show(error.Message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Error); }
        return false;
    }
}
