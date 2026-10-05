# Renders the real application offscreen and saves PNGs to tools\render\out\:
#   creature_<name>.png - every creature card, header included: the bundled SRD pack and the test
#                         fixture packs (tests\Fixtures\Packs)
#   gear_<name>.png     - every gear card: the bundled SRD pack and the test fixture packs
#   condition_<name>.png - every condition card: the bundled SRD pack
#   tab_<type>.png      - each content tab whole, list included, its first row selected
#   gallery_<section>.png - every section of the controls gallery
# Usage (from anywhere):
#   powershell -File tools\render\render.ps1                       # everything
#   powershell -File tools\render\render.ps1 -Only 'condition_*'   # only files matching the wildcard
#   powershell -File tools\render\render.ps1 -Only 'creature_szkielet'
# A full run selects every row and takes minutes; -Only renders just the view that changed.
# out\ is emptied first either way, so it never holds a picture from an earlier run.
# Build output goes to tools\render\artifacts\ (UseArtifactsOutput, for the referenced projects too),
# so the author's bin\ folders, which a running app or the XAML preview may lock, stay untouched.
param([string]$Only)

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
Remove-Item (Join-Path $out '*.png') -ErrorAction SilentlyContinue
$fixturePacks = Join-Path $here '..\..\tests\Fixtures\Packs'
$renderArgs = @($out, $fixturePacks)
if ($Only) { $renderArgs += "--only=$Only" }
& $dotnet (Join-Path $artifacts 'bin\Render\debug\Render.dll') @renderArgs
if ($LASTEXITCODE -ne 0) { throw 'render failed' }
