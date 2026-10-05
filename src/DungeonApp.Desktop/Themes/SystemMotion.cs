using System;
using System.Runtime.InteropServices;
using Avalonia.Markup.Xaml.Styling;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Single shell location reading the system animation setting: when Windows animations are disabled
/// ("Pokaż animacje w systemie Windows"), includes ReducedMotion.axaml styles at startup to
/// remove shell-theme transitions. Reads once; setting changes take effect on the next launch.
/// Code-based motion (indeterminate progress, confirmation dialog, notification) checks
/// <see cref="IsReduced"/> - result of the same single read.
/// </summary>
internal static class SystemMotion
{
    private const uint SpiGetClientAreaAnimation = 0x1042;

    /// <summary>Windows animations are disabled (determined once in <see cref="Apply"/>).</summary>
    public static bool IsReduced { get; private set; }

    public static void Apply(Avalonia.Application application)
    {
        if (OperatingSystem.IsWindows()
            && SystemParametersInfoW(SpiGetClientAreaAnimation, 0, out var enabled, 0)
            && enabled == 0)
        {
            IsReduced = true;
            application.Styles.Add(new StyleInclude(new Uri("avares://DungeonApp.Desktop/"))
            {
                Source = new Uri("avares://DungeonApp.Desktop/Themes/ReducedMotion.axaml"),
            });
        }
    }

    [DllImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfoW(uint action, uint param, out int value, uint winIni);
}
