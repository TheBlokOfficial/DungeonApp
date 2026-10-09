# Builds the solution and runs the tests, printing only what needs attention: compiler errors and
# warnings, failed tests with their messages (no stack traces), and one summary line per test project.
# Each line of full MSBuild/VSTest output is resent to an agent on every later step, so it is kept out.
#
# Usage (from anywhere):
#   powershell -File tools\check.ps1                            # build + all tests
#   powershell -File tools\check.ps1 -Filter Vocabulary         # tests whose full name contains the text
#   powershell -File tools\check.ps1 -Tests Core,Architecture   # only these test projects
#   powershell -File tools\check.ps1 -Tests Desktop.RenderingTests
#   powershell -File tools\check.ps1 -NoTest                    # build only
# -Filter also takes a full dotnet test expression ("FullyQualifiedName~X|Category=Y").
#
# MSB3021/MSB3027 on the app's bin\ means the author's running app or Rider's XAML preview holds the
# file; the script then builds and tests the test projects alone, which do not depend on the app.
# MSB3026 is the retry warning MSBuild prints before giving up with those two, so it counts as well.
param(
    [string]$Filter,
    [string[]]$Tests,
    [switch]$NoTest
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$solution = Join-Path $root 'DungeonApp.sln'

$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
if (-not $dotnet) {
    $dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
    if (-not (Test-Path $dotnet)) { throw 'dotnet not found on PATH nor in %USERPROFILE%\.dotnet' }
    # Test hosts look the runtime up through DOTNET_ROOT when dotnet is not installed machine-wide.
    $env:DOTNET_ROOT = Split-Path $dotnet -Parent
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
}
# English output keeps the patterns below independent of the machine's language.
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$env:DOTNET_NOLOGO = '1'

$testProjects = Get-ChildItem (Join-Path $root 'tests') -Filter '*Tests.csproj' -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
if ($Tests) {
    $testProjects = $testProjects | Where-Object {
        $name = $_.BaseName
        $Tests | Where-Object { $name -like "DungeonApp.$_.Tests" -or $name -like "DungeonApp.$_" -or $name -eq $_ }
    }
    if (-not $testProjects) { throw "No test project matches: $($Tests -join ', ')" }
}

function Invoke-Build([string]$target) {
    $lines = & $dotnet build $target -nologo -v:q -tl:off '-clp:NoSummary'
    [pscustomobject]@{
        Ok    = ($LASTEXITCODE -eq 0)
        # One diagnostic can be reported once per target framework or project; show it once.
        Lines = @($lines | Where-Object { $_ -match ': (error|warning) [A-Z]+\d+' } | Select-Object -Unique)
    }
}

$build = Invoke-Build $solution
$locked = $build.Lines.Count -gt 0 -and -not ($build.Lines | Where-Object { $_ -notmatch 'MSB30(21|26|27)' })
if ($locked) {
    Write-Output 'Build: the app''s bin\ is locked (running app or XAML preview); building the test projects alone.'
    $build = [pscustomobject]@{ Ok = $true; Lines = @() }
    foreach ($project in $testProjects) {
        $part = Invoke-Build $project.FullName
        $build.Ok = $build.Ok -and $part.Ok
        $build.Lines += $part.Lines
    }
    $build.Lines = @($build.Lines | Select-Object -Unique)
}

$build.Lines | ForEach-Object { Write-Output $_ }
$errors = @($build.Lines | Where-Object { $_ -match ': error ' }).Count
$warnings = @($build.Lines | Where-Object { $_ -match ': warning ' }).Count
Write-Output "Build: $errors error(s), $warnings warning(s)"
if (-not $build.Ok) {
    if ($errors -eq 0) { Write-Output 'Build failed without a compiler diagnostic; rerun dotnet build without -v:q to see why.' }
    exit 1
}
if ($NoTest) { exit 0 }

$filterArgs = @()
if ($Filter) {
    $expression = if ($Filter -match '[=~|&!]') { $Filter } else { "FullyQualifiedName~$Filter" }
    $filterArgs = @('--filter', $expression)
}

$failed = $false
$summaries = 0
$targets = if ($Tests -or $locked) { $testProjects.FullName } else { @($solution) }
foreach ($target in $targets) {
    # -v:q would also silence the console logger's failure messages; minimal keeps them.
    $lines = & $dotnet test $target --no-build -nologo -tl:off --logger 'console;verbosity=minimal' @filterArgs
    if ($LASTEXITCODE -ne 0) { $failed = $true }

    # Keep each failed test's name, its message and the first stack line (the test's file and
    # line), and the per-project summaries; drop the rest of the trace and captured output.
    $section = ''
    foreach ($line in $lines) {
        if ($line -match '^\s*Failed \S' -or $line -match '^(Passed|Failed)!') {
            Write-Output $line
            $section = ''
            if ($line -match '^(Passed|Failed)!') { $summaries++ }
        }
        elseif ($line -match '^\s*Error Message:') { $section = 'message' }
        elseif ($line -match '^\s*Stack Trace:') { $section = 'stack' }
        elseif ($line -match '^\s*Standard (Output|Error) Messages:') { $section = '' }
        elseif ($section -eq 'message' -and $line.Trim()) { Write-Output "    $($line.Trim())" }
        elseif ($section -eq 'stack' -and $line.Trim()) { Write-Output "    $($line.Trim())"; $section = '' }
    }
}

if ($summaries -eq 0) { Write-Output 'No test ran: nothing matches the filter.' }
if ($failed) { exit 1 }
