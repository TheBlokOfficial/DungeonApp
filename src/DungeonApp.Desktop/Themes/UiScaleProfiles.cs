using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Themes;

public static class UiScaleProfiles
{
    /// <summary>
    /// Parses a one-run <c>--ui-scale=</c> override from the command line. Returns null when no
    /// override was given, so the caller falls back to the persisted
    /// <see cref="DungeonApp.Desktop.Settings.AppSettings"/> profile instead of a hardcoded default.
    /// </summary>
    public static UiScaleProfile? Parse(IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            const string prefix = "--ui-scale=";
            if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return Enum.TryParse<UiScaleProfile>(argument[prefix.Length..], true, out var profile)
                ? profile
                : null;
        }

        return null;
    }

    public static void Apply(Avalonia.Application application, UiScaleProfile profile)
    {
        var metrics = profile switch
        {
            UiScaleProfile.Small => new UiMetrics(
                BaseFont: 14,
                NavigationFont: 14,
                HelperFont: 12,
                EyebrowFont: 11,
                StatusFont: 11,
                WorkspaceTitleFont: 24,
                TopBarHeight: 48,
                SidebarWidth: 216,
                StatusBarHeight: 24,
                WorkspaceHeaderHeight: 64,
                ControlHeight: 36,
                NavigationRowHeight: 40,
                ActionIconSize: 16,
                PanelHeaderHeight: 34,
                PanelTitleFont: 12,
                PanelMinWidth: 240,
                PanelMinHeight: 144,
                PanelResizeBorderThickness: 6,
                PanelResizeCornerSize: 12,
                DeckCardSize: 40),
            UiScaleProfile.Large => new UiMetrics(
                BaseFont: 17,
                NavigationFont: 17,
                HelperFont: 14,
                EyebrowFont: 12,
                StatusFont: 12,
                WorkspaceTitleFont: 29,
                TopBarHeight: 56,
                SidebarWidth: 248,
                StatusBarHeight: 28,
                WorkspaceHeaderHeight: 76,
                ControlHeight: 42,
                NavigationRowHeight: 46,
                ActionIconSize: 18,
                PanelHeaderHeight: 40,
                PanelTitleFont: 15,
                PanelMinWidth: 260,
                PanelMinHeight: 168,
                PanelResizeBorderThickness: 8,
                PanelResizeCornerSize: 14,
                DeckCardSize: 48),
            _ => new UiMetrics(
                BaseFont: 15,
                NavigationFont: 15,
                HelperFont: 13,
                EyebrowFont: 11,
                StatusFont: 11,
                WorkspaceTitleFont: 26,
                TopBarHeight: 52,
                SidebarWidth: 224,
                StatusBarHeight: 24,
                WorkspaceHeaderHeight: 68,
                ControlHeight: 38,
                NavigationRowHeight: 42,
                ActionIconSize: 16,
                PanelHeaderHeight: 36,
                PanelTitleFont: 13,
                PanelMinWidth: 240,
                PanelMinHeight: 152,
                PanelResizeBorderThickness: 6,
                PanelResizeCornerSize: 12,
                DeckCardSize: 44)
        };

        Set(application, "DungeonBaseFontSize", metrics.BaseFont);
        Set(application, "DungeonNavigationFontSize", metrics.NavigationFont);
        Set(application, "DungeonHelperFontSize", metrics.HelperFont);
        Set(application, "DungeonEyebrowFontSize", metrics.EyebrowFont);
        Set(application, "DungeonStatusFontSize", metrics.StatusFont);
        Set(application, "DungeonWorkspaceTitleFontSize", metrics.WorkspaceTitleFont);
        Set(application, "DungeonBrandMarkFontSize", metrics.BaseFont + 3);

        Set(application, "DungeonTopBarHeight", new GridLength(metrics.TopBarHeight));
        Set(application, "DungeonSidebarWidth", new GridLength(metrics.SidebarWidth));
        Set(application, "DungeonStatusBarHeight", new GridLength(metrics.StatusBarHeight));
        Set(application, "DungeonWorkspaceHeaderHeight", metrics.WorkspaceHeaderHeight);
        Set(application, "DungeonControlHeight", metrics.ControlHeight);
        Set(application, "DungeonNavigationRowHeight", metrics.NavigationRowHeight);
        Set(application, "DungeonActionIconSize", metrics.ActionIconSize);
        Set(application, "DungeonTopBarActionSize", metrics.TopBarHeight);
        Set(application, "DungeonMinimumWindowWidth", profile == UiScaleProfile.Large ? 1180d : 1024d);
        Set(application, "DungeonMinimumWindowHeight", profile == UiScaleProfile.Large ? 760d : 680d);

        Set(application, "DungeonPanelHeaderHeight", metrics.PanelHeaderHeight);
        Set(application, "DungeonPanelHeaderActionSize", metrics.PanelHeaderHeight);
        Set(application, "DungeonPanelTitleFontSize", metrics.PanelTitleFont);
        Set(application, "DungeonPanelResizeBorderThickness", metrics.PanelResizeBorderThickness);
        Set(application, "DungeonPanelResizeCornerSize", metrics.PanelResizeCornerSize);
        Set(application, "DungeonDeckCardSize", metrics.DeckCardSize);

        // Read back by Controls/Workspace/WorkspaceMetricsResolver on every gesture and surface
        // resize. Changing a profile therefore changes the panel size floor even when the desk
        // itself did not change size, so the workspace has to re-fit its panels on profile change
        // and not only on SizeChanged.
        Set(application, "DungeonPanelMinWidth", metrics.PanelMinWidth);
        Set(application, "DungeonPanelMinHeight", metrics.PanelMinHeight);

        // Profile-independent, but sizes all the same, so they stay with the single size writer
        // rather than drifting into Tokens.axaml. Values from docs/ui/contract.md.
        Set(application, "DungeonWorkspacePadding", 16d);
        // Thickness twin of the value above, for views that need it as a Margin. Kept next to its
        // source so the two can never disagree.
        Set(application, "DungeonWorkspacePaddingThickness", new Avalonia.Thickness(16));
        Set(application, "DungeonPanelGap", 8d);
        Set(application, "DungeonDeckCardSpacing", 8d);
    }

    private static void Set(Avalonia.Application application, string key, object value) =>
        application.Resources[key] = value;

    private sealed record UiMetrics(
        double BaseFont,
        double NavigationFont,
        double HelperFont,
        double EyebrowFont,
        double StatusFont,
        double WorkspaceTitleFont,
        double TopBarHeight,
        double SidebarWidth,
        double StatusBarHeight,
        double WorkspaceHeaderHeight,
        double ControlHeight,
        double NavigationRowHeight,
        double ActionIconSize,
        double PanelHeaderHeight,
        double PanelTitleFont,
        double PanelMinWidth,
        double PanelMinHeight,
        double PanelResizeBorderThickness,
        double PanelResizeCornerSize,
        double DeckCardSize);
}
