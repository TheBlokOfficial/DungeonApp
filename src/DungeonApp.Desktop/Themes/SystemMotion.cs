using System;
using System.Runtime.InteropServices;
using Avalonia.Markup.Xaml.Styling;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Jedno miejsce w ramie, które czyta systemowe ustawienie animacji: gdy Windows ma je wyłączone
/// ("Pokaż animacje w systemie Windows"), przy starcie dołącza style ReducedMotion.axaml, które
/// zdejmują przejścia motywu ramy. Czyta raz; zmiana ustawienia działa od następnego uruchomienia.
/// Ruch żyjący w kodzie (wskaźnik postępu nieokreślony, okno potwierdzenia, powiadomienie) sprawdza
/// <see cref="IsReduced"/> - wynik tego samego jednego odczytu.
/// </summary>
internal static class SystemMotion
{
    private const uint SpiGetClientAreaAnimation = 0x1042;

    /// <summary>Windows ma animacje wyłączone (ustalone raz, w <see cref="Apply"/>).</summary>
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
