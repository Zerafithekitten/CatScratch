using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

// An owned, nonactivating input surface keeps just the speech bubble clickable
// while the companion itself continues passing clicks through to the game.
internal sealed class CompanionUpdateBubble : Window
{
    internal Button UpdateButton { get; }
    internal CompanionUpdateBubble(SupporterCatWindow owner, Action openUpdate)
    {
        Owner=owner;Title="CatScratch · Update";WindowStyle=WindowStyle.None;
        ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
        ShowInTaskbar=false;ShowActivated=false;Topmost=true;SizeToContent=SizeToContent.WidthAndHeight;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        var root=new Grid {Width=132,Height=54};
        var tail=new System.Windows.Shapes.Path {Data=Geometry.Parse("M 24,40 L 17,53 L 43,40 Z"),Fill=ThemeColors.Brush("#20242A"),Stroke=ThemeColors.Brush("#C49AF5"),StrokeThickness=1.2,IsHitTestVisible=false};
        root.Children.Add(tail);
        UpdateButton=MeterControlsWindow.CreateInlineUpdateButton(this);
        UpdateButton.FontSize=13;UpdateButton.Background=Brushes.Transparent;
        UpdateButton.Foreground=ThemeColors.Brush("#EDF2F7");UpdateButton.Content="↑  Update!";
        UpdateButton.Click+=(_,_)=>openUpdate();
        root.Children.Add(new Border {Height=42,VerticalAlignment=VerticalAlignment.Top,CornerRadius=new(12),Background=ThemeColors.Brush("#20242A"),BorderBrush=ThemeColors.Brush("#C49AF5"),BorderThickness=new(1.2),Child=UpdateButton});
        Content=root;WindowPresentation.Attach(this);
        SourceInitialized+=(_,_)=>
        {
            var handle=new WindowInteropHelper(this).Handle;
            NativeMethods.SetExStyle(handle,NativeMethods.WS_EX_TOOLWINDOW|NativeMethods.WS_EX_NOACTIVATE,0x00040000);
            HwndSource.FromHwnd(handle)?.AddHook((nint h,int message,nint w,nint l,ref bool handled)=>
            {if(message==0x0021){handled=true;return 3;}return 0;});
        };
    }
    internal void Follow(SupporterCatWindow cat)
    {
        Left=cat.Left+cat.ActualWidth*.57;
        Top=cat.Top+24+cat.CatSize*.14-ActualHeight;
        Opacity=cat.Opacity;
        UpdateButton.ToolTip=UiText.Current.TipUpdate;
        WindowPresentation.Constrain(this);
    }
}
