using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace AionMeter.App.Services;

/// <summary>Refresh only shortcuts targeting this installation, without changing their targets or pin state.</summary>
internal static class ShortcutIcons
{
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)]
    private static extern void SHChangeNotify(uint change,uint flags,string? item,nint second);

    internal static void Refresh(string installation)
    {
        object? shell=null;
        try
        {
            var exe=Path.Combine(Path.GetFullPath(installation),"CatScratch.exe");
            var icon=Path.Combine(installation,"CatScratch.ico");
            using(var input=Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"))!.Stream)
            using(var output=File.Create(icon))input.CopyTo(output);
            shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
            var folders=new[]{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Microsoft","Internet Explorer","Quick Launch","User Pinned","TaskBar")};
            foreach(var folder in folders.Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
            foreach(var path in Directory.EnumerateFiles(folder,"*.lnk",SearchOption.TopDirectoryOnly))
            {
                object? link=null;
                try
                {
                    dynamic shortcut=link=((dynamic)shell!).CreateShortcut(path);
                    string target=shortcut.TargetPath;
                    if(!string.Equals(Path.GetFullPath(target),exe,StringComparison.OrdinalIgnoreCase))continue;
                    shortcut.IconLocation=icon+",0";
                    shortcut.Save();
                    SHChangeNotify(0x2000,0x5,path,0); // SHCNE_UPDATEITEM, SHCNF_PATHW
                }
                catch(Exception error){Log.Info("Shortcut icon refresh skipped: "+error.GetType().Name);}
                finally{if(link is not null && Marshal.IsComObject(link))Marshal.FinalReleaseComObject(link);}
            }
            SHChangeNotify(0x2000,0x5,exe,0);
            SHChangeNotify(0x08000000,0,null,0); // Refresh the shell's icon associations.
        }
        catch(Exception error){Log.Info("Shell icon refresh skipped: "+error.GetType().Name);}
        finally{if(shell is not null && Marshal.IsComObject(shell))Marshal.FinalReleaseComObject(shell);}
    }
}
