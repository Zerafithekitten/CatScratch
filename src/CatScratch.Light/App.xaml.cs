using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using AionMeter.App.Services;

namespace AionMeter.App.Services { internal static class AppInfo { public const string Name="CatScratch light"; } }

namespace CatScratch.Light
{
public partial class App : Application
{
    private Mutex? _instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if(e.Args.Contains("--verify") || e.Args.Contains("--render"))
        {
            try { Verification.Run(e.Args);Shutdown(); }
            catch(Exception error){File.WriteAllText(e.Args.Last()+".error.txt",error.ToString());Shutdown(1);}
            return;
        }
        if(!AdministratorStartup.Ensure(e.Args)){Shutdown();return;}
        _instance=new Mutex(true,"CatScratch.Light.SingleInstance",out var first);
        if(!first){MessageBox.Show("CatScratch light is already running.","CatScratch light");Shutdown();return;}
        DispatcherUnhandledException+=(_,a)=>{MessageBox.Show(a.Exception.Message,"CatScratch light");a.Handled=true;};
        var window=new MeterWindow(LightSettings.Load());
        MainWindow=window;window.Show();window.StartCapture();
    }
    protected override void OnExit(ExitEventArgs e){_instance?.Dispose();base.OnExit(e);}
    internal static ImageSource Paw {get;}=LoadPaw();
    private static ImageSource LoadPaw()
    {
        var image=(DrawingImage)LoadComponent(new Uri("/CatScratch.Light;component/Assets/CatPaw.xaml",UriKind.Relative));image.Freeze();return image;
    }
}
}

