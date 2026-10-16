# Rebuild the outlined purple paw at native Windows icon sizes; no external tools or downloads.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$asset = Join-Path $PSScriptRoot '../src/AionMeter.App/Assets'
$reader = [Xml.XmlReader]::Create((Join-Path $asset 'CatPaw.xaml'))
try { $drawing = ([System.Windows.Markup.XamlReader]::Load($reader)).Drawing }
finally { $reader.Dispose() }
$frames = @()
foreach ($size in @(16,20,24,32,40,48,64,128,256)) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    $dc = $visual.RenderOpen()
    $dc.PushTransform((New-Object System.Windows.Media.ScaleTransform ($size/24.0),($size/24.0)))
    $dc.DrawDrawing($drawing)
    $dc.Pop(); $dc.Close()
    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size,$size,96,96,([System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $buffer = New-Object System.IO.MemoryStream
    $encoder.Save($buffer)
    $frames += ,@($size, $buffer.ToArray())
    if ($size -eq 128) { [IO.File]::WriteAllBytes((Join-Path $asset 'paw-preview.png'),$buffer.ToArray()) }
    $buffer.Dispose()
}
$file = [IO.File]::Create((Join-Path $asset 'app.ico'))
$writer = New-Object IO.BinaryWriter $file
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$frames.Count)
    $offset = 6 + 16*$frames.Count
    foreach($frame in $frames) {
        $dimension = if($frame[0] -eq 256){0}else{$frame[0]}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame[1].Length);$writer.Write([uint32]$offset)
        $offset += $frame[1].Length
    }
    foreach($frame in $frames){$writer.Write([byte[]]$frame[1])}
} finally { $writer.Dispose(); $file.Dispose() }
