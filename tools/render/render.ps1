# Renders the real application offscreen and saves PNGs to tools\render\out\:
#   creature_<name>.png - every creature card, header included: the bundled SRD pack and the test
#                         fixture packs (tests\Fixtures\Packs)
#   gear_<name>.png     - every gear card: the bundled SRD pack and the test fixture packs
#   condition_<name>.png - every condition card: the bundled SRD pack
#   tab_<type>.png      - each content tab whole, list included, its first row selected
#   icons_sheet.png     - the system's icons side by side (conditions, creature types, item
#                         categories) at row and card sizes, beside the shell's Lucide skull
#   gallery_<section>.png - every section of the controls gallery
#   sidebar_shelf.png, sidebar_campaign.png - the sidebar with no campaign open (the shelf) and with a
#                         freshly opened one ("Kopalnia Phandelver", made in a throwaway documents
#                         folder, never the author's), whose desk is the campaign position
#   desk_empty.png, desk_window.png - the campaign desk, its command strip included, with no window
#                         and with the world window
#   window_full.png     - the whole application window with the desk and the open window
# Usage (from anywhere):
#   powershell -File tools\render\render.ps1                       # everything
#   powershell -File tools\render\render.ps1 -Only 'condition_*'   # only files matching the wildcard
#   powershell -File tools\render\render.ps1 -Only 'entity_card_creature,entity_card_creature_edit,gallery_*'
#   powershell -File tools\render\render.ps1 -Only 'entity_card_creature_edit' -Zoom 4 -Control PART_Field
# A full run selects every row and takes minutes; -Only renders just the views that changed, all of
# them after one build. It takes names and wildcards, separated by commas (or as a PowerShell array),
# and names every pattern that matched nothing - a misspelt scenario is otherwise skipped silently.
# -Zoom <scale> saves next to each picture <name>.zoom.png (then .zoom-2.png...): a crop enlarged
# without smoothing, so single-pixel lines and frames can be judged. What is cropped:
#   -Control <names> - each visible control with that x:Name (template parts like PART_Field count)
#                      or type name, with everything its parts draw (a frame pushed out by a negative
#                      margin too) and 8 units around it
#   -Area x,y,w,h    - a rectangle in the picture's pixels
#   neither          - the control that has the focus, if the picture holds it
# -Control or -Area alone zooms 4 times. The scale multiplies the picture's pixels, and a scaled
# picture (entity_card_*, gallery_live-part*) is already twice the size.
# out\ keeps pictures from earlier runs and the run overwrites only what it renders, so a picture
# stays until it is looked at; -Clean empties out\ first.
# Build output goes to tools\render\artifacts\ (UseArtifactsOutput, for the referenced projects too),
# so the author's bin\ folders, which a running app or the XAML preview may lock, stay untouched.
param(
    [string[]]$Only,
    [int]$Zoom,
    [string[]]$Control,
    [string[]]$Area,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) {
    $dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
    $env:DOTNET_ROOT = Split-Path $dotnet -Parent
}
if (-not (Test-Path $dotnet)) {
    throw 'dotnet not found on PATH nor in %USERPROFILE%\.dotnet'
}

$artifacts = Join-Path $here 'artifacts'
$out = Join-Path $here 'out'

$build = & $dotnet build (Join-Path $here 'Render.csproj') -c Debug -nologo -v:q -tl:off `
    -p:UseArtifactsOutput=true "-p:ArtifactsPath=$artifacts"
if ($LASTEXITCODE -ne 0) {
    $build | Where-Object { $_ -match ': (error|warning) ' } | Select-Object -Unique
    throw 'build failed'
}

New-Item -ItemType Directory -Force $out | Out-Null
if ($Clean) { Remove-Item (Join-Path $out '*.png') -ErrorAction SilentlyContinue }
$fixturePacks = Join-Path $here '..\..\tests\Fixtures\Packs'
$renderArgs = @($out, $fixturePacks)
# powershell -File hands 'a,b' over as one string, a call from PowerShell as an array: both end as a list.
foreach ($pattern in $Only) { $renderArgs += "--only=$pattern" }
foreach ($name in $Control) { $renderArgs += "--zoom-control=$name" }
if ($Area) { $renderArgs += "--zoom-area=$($Area -join ',')" }
if (-not $Zoom -and ($Control -or $Area)) { $Zoom = 4 }
if ($Zoom) { $renderArgs += "--zoom=$Zoom" }
& $dotnet (Join-Path $artifacts 'bin\Render\debug\Render.dll') @renderArgs
if ($LASTEXITCODE -ne 0) { throw 'render failed' }
