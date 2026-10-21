using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Controls;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;

/// <summary>Static identity decoration; never changes layout, focus, hit targets or saved geometry.</summary>
public static class CatBranding
{
    public static ImageSource Wordmark {get;}=Load("wordmark.png",640);
    public static ImageSource Mascot {get;}=Load("mascot.png",128);
    private static ImageSource Load(string file,int pixels)
    {
        var image=new BitmapImage();image.BeginInit();image.UriSource=new Uri("pack://application:,,,/CatScratch;component/Assets/Branding/"+file);image.DecodePixelWidth=pixels;image.CacheOption=BitmapCacheOption.OnLoad;image.EndInit();image.Freeze();return image;
    }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Window,SignatureAdorner> Adornments=new();
    private static bool _registered;
    internal static void Register()
    {
        if(_registered)return;_registered=true;
        EventManager.RegisterClassHandler(typeof(Window),FrameworkElement.LoadedEvent,new RoutedEventHandler((sender,_)=>Apply((Window)sender)));
    }
    internal static bool IsBranded(Window window)=>Adornments.TryGetValue(window,out _);
    internal static void Apply(Window window)
    {
        if(window.GetType().Namespace!=typeof(SettingsWindow).Namespace || window is HiddenCubeWindow or SupporterCatWindow)return;
        if(window.Content is not FrameworkElement root)return;
        var compact=window is IDisplayAppearance or OverlayWindow or MeterControlsWindow;
        RefreshAssets(root,!compact);
        if(Adornments.TryGetValue(window,out _))return;
        root=Target(window)??root;
        if(AdornerLayer.GetAdornerLayer(root) is not {} layer)return;
        var adorner=new SignatureAdorner(root,compact);layer.Add(adorner);Adornments.Add(window,adorner);
        root.AddHandler(ScrollViewer.ScrollChangedEvent,new ScrollChangedEventHandler((_,_)=>adorner.InvalidateVisual()));
        if(window is SettingsWindow settings && settings.FindName("SettingsTabs") is TabControl tabs)
            tabs.SelectionChanged+=(_,_)=>adorner.InvalidateVisual();
        window.Closed+=(_,_)=>{layer.Remove(adorner);Adornments.Remove(window);};
    }
    private static void RefreshAssets(DependencyObject node,bool headings)
    {
        if(node is CatWordmark title)title.RefreshArtwork();
        if(node is ButtonBase or ItemsControl)return;
        if(headings && node is Image image && ReferenceEquals(image.Source,AppIcon.Image))image.Source=Mascot;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)RefreshAssets(VisualTreeHelper.GetChild(node,i),headings);
    }
    internal static FrameworkElement? Target(Window window)
    {
        if(window is UpdateWindow update)return update.FindName("BrandNotesSurface") as FrameworkElement;
        if(window is SettingsWindow settings && settings.FindName("SettingsTabs") is TabControl tabs)
        {
            tabs.ApplyTemplate();
            return tabs.Template.FindName("PART_SelectedContentHost",tabs) as FrameworkElement;
        }
        return window.Content as FrameworkElement;
    }
    internal static void DrawPreview(Window window,DrawingContext drawing,Size size)
    {
        if(window is HiddenCubeWindow or SupporterCatWindow || Target(window) is not {} target || window.Content is not FrameworkElement root)return;
        var origin=target==root?new Point():target.TranslatePoint(new Point(),root);
        drawing.PushTransform(new TranslateTransform(origin.X,origin.Y));
        DrawSignature(drawing,target,window is IDisplayAppearance or OverlayWindow or MeterControlsWindow);
        drawing.Pop();
    }
    // Do not paint over text, inputs or controls, even in a short or densely populated window.
    private static Geometry ClearContent(FrameworkElement root)
    {
        Geometry area=new RectangleGeometry(new Rect(root.RenderSize));
        void Visit(DependencyObject node)
        {
            if(node is UIElement element && element.Visibility!=Visibility.Visible)return;
            if(node is FrameworkElement control && node is ButtonBase or TextBox or ComboBox or Slider or ScrollBar or TextBlock)
            {
                if(control.RenderSize.Width>0&&control.RenderSize.Height>0)
                {
                    var local=new Rect(control.RenderSize);
                    if(control is TextBlock text)
                    {
                        var width=Math.Min(text.RenderSize.Width,text.DesiredSize.Width);var height=Math.Min(text.RenderSize.Height,text.DesiredSize.Height);
                        local=new Rect(text.TextAlignment==TextAlignment.Right?text.ActualWidth-width:text.TextAlignment==TextAlignment.Center?(text.ActualWidth-width)/2:0,0,width,height);
                    }
                    var bounds=control.TransformToAncestor(root).TransformBounds(local);bounds.Inflate(2,2);
                    area=new CombinedGeometry(GeometryCombineMode.Exclude,area,new RectangleGeometry(bounds));
                }
                return;
            }
            for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)Visit(VisualTreeHelper.GetChild(node,i));
        }
        Visit(root);area.Freeze();return area;
    }
    private static void DrawSignature(DrawingContext drawing,FrameworkElement target,bool compact)
    {
        var size=target.RenderSize;
        if(size.Width<40||size.Height<35)return;
        var width=compact?Math.Min(68,Math.Min(size.Width*.25,size.Height*.8)):Math.Min(220,Math.Min(size.Width*.32,size.Height*.5));
        var height=width*90/98;var margin=compact?3:14;
        drawing.PushClip(ClearContent(target));
        drawing.PushOpacity(compact?.10:.18);
        drawing.DrawImage(CatSignature.Artwork,new Rect(Math.Max(0,size.Width-width-margin),Math.Max(0,size.Height-height-margin),width,height));
        if(!compact)
        {
            var logoWidth=Math.Min(110,width*.6);
            drawing.DrawImage(Wordmark,new Rect(size.Width-logoWidth-margin,size.Height-logoWidth/3-margin,logoWidth,logoWidth/3));
        }
        drawing.Pop();drawing.Pop();
    }
    private sealed class SignatureAdorner : Adorner
    {
        private readonly bool _compact;
        internal SignatureAdorner(UIElement target,bool compact):base(target){_compact=compact;IsHitTestVisible=false;Focusable=false;}
        protected override void OnRender(DrawingContext drawing)
        {
            DrawSignature(drawing,(FrameworkElement)AdornedElement,_compact);
        }
    }
}
