# Enlarges a region of a rendered PNG without smoothing, so single-pixel lines can be checked:
#   powershell -File tools\render\zoom.ps1 <source.png> <x> <y> <width> <height> <scale> <target.png>
param(
    [Parameter(Mandatory)] [string] $Source,
    [Parameter(Mandatory)] [int] $X,
    [Parameter(Mandatory)] [int] $Y,
    [Parameter(Mandatory)] [int] $Width,
    [Parameter(Mandatory)] [int] $Height,
    [Parameter(Mandatory)] [int] $Scale,
    [Parameter(Mandatory)] [string] $Target
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$bitmap = [Drawing.Bitmap]::FromFile((Resolve-Path $Source))
$zoomed = New-Object Drawing.Bitmap ($Width * $Scale), ($Height * $Scale)
$graphics = [Drawing.Graphics]::FromImage($zoomed)
$graphics.InterpolationMode = 'NearestNeighbor'
$graphics.PixelOffsetMode = 'Half'
$graphics.DrawImage(
    $bitmap,
    (New-Object Drawing.Rectangle 0, 0, $zoomed.Width, $zoomed.Height),
    (New-Object Drawing.Rectangle $X, $Y, $Width, $Height),
    'Pixel')
$zoomed.Save($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Target))
$graphics.Dispose()
$bitmap.Dispose()
$zoomed.Dispose()
