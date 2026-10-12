. "$PSScriptRoot/toolchain.ps1"
Add-Type -AssemblyName PresentationCore,WindowsBase
$taskIconDirectory = Join-Path $taskRoot 'src/remote/public'
$taskSourcePath = Join-Path $taskRoot 'assets/branding/pc-remote-logo-v2.png'
$taskSource = [System.Windows.Media.Imaging.BitmapImage]::new()
$taskSource.BeginInit()
$taskSource.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
$taskSource.UriSource = [Uri]::new($taskSourcePath)
$taskSource.EndInit()
$taskSource.Freeze()
if ($taskSource.PixelWidth -ne $taskSource.PixelHeight) { throw 'The logo master must be square.' }

function Write-TaskIconPng([int]$Size, [string]$Path, [double]$Inset = 0) {
    $taskVisual = [System.Windows.Media.DrawingVisual]::new()
    [System.Windows.Media.RenderOptions]::SetBitmapScalingMode($taskVisual, [System.Windows.Media.BitmapScalingMode]::HighQuality)
    $taskDrawing = $taskVisual.RenderOpen()
    $taskDrawing.DrawRectangle([System.Windows.Media.BrushConverter]::new().ConvertFromString('#0b0f14'), $null, [System.Windows.Rect]::new(0,0,$Size,$Size))
    $taskPadding = $Size * $Inset
    $taskDrawing.DrawImage($taskSource, [System.Windows.Rect]::new($taskPadding,$taskPadding,$Size-2*$taskPadding,$Size-2*$taskPadding))
    $taskDrawing.Close()
    $taskBitmap = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($Size,$Size,96,96,[System.Windows.Media.PixelFormats]::Pbgra32)
    $taskBitmap.Render($taskVisual)
    $taskEncoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $taskEncoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($taskBitmap))
    $taskStream = [IO.File]::Create($Path)
    try { $taskEncoder.Save($taskStream) } finally { $taskStream.Dispose() }
}

# Full-bleed opaque squares: iOS and Windows apply their own presentation masks.
foreach ($taskSize in @(16,24,32,48,64,128,180,192,256,512)) {
    Write-TaskIconPng $taskSize (Join-Path $taskIconDirectory "pc-remote-v2-$taskSize.png")
}
# Extra padding keeps the complete mark inside the maskable icon safe circle.
Write-TaskIconPng 512 (Join-Path $taskIconDirectory 'pc-remote-v2-maskable-512.png') 0.12

# Multiple ICO frames keep the tray crisp and avoid enlarging a 32px shortcut icon.
$taskIconSizes = @(16,24,32,48,64,128,256)
$taskFrames = @($taskIconSizes | ForEach-Object { ,([IO.File]::ReadAllBytes((Join-Path $taskIconDirectory "pc-remote-v2-$_.png"))) })
$taskStream = [IO.File]::Create((Join-Path $taskIconDirectory 'pc-remote-v2.ico'))
$taskWriter = [IO.BinaryWriter]::new($taskStream)
try {
    $taskWriter.Write([uint16]0); $taskWriter.Write([uint16]1); $taskWriter.Write([uint16]$taskFrames.Count)
    $taskOffset = 6 + 16 * $taskFrames.Count
    for ($taskIndex = 0; $taskIndex -lt $taskFrames.Count; $taskIndex++) {
        $taskDimension = if ($taskIconSizes[$taskIndex] -eq 256) { 0 } else { $taskIconSizes[$taskIndex] }
        $taskWriter.Write([byte]$taskDimension); $taskWriter.Write([byte]$taskDimension)
        $taskWriter.Write([byte]0); $taskWriter.Write([byte]0)
        $taskWriter.Write([uint16]1); $taskWriter.Write([uint16]32)
        $taskWriter.Write([uint32]$taskFrames[$taskIndex].Length); $taskWriter.Write([uint32]$taskOffset)
        $taskOffset += $taskFrames[$taskIndex].Length
    }
    foreach ($taskFrame in $taskFrames) { $taskWriter.Write([byte[]]$taskFrame) }
} finally { $taskWriter.Dispose() }
Write-Output 'Generated PC Remote v2 PNGs and Windows ICO from assets/branding/pc-remote-logo-v2.png.'
