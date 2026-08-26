using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using DungeonApp.Desktop.Shell.Sidebars;

namespace DungeonApp.Desktop.Themes;

public static class UiScaleProfiles
{
    /// <summary>Sidebar width in its compact variant, independent of the active scale profile.</summary>
    private const double CompactSidebarWidth = 56;

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

    public static void Apply(Avalonia.Application application, UiScaleProfile profile, SidebarVariant sidebarVariant)
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
                CampaignRowHeight: 72,
                ActionIconSize: 16),
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
                CampaignRowHeight: 84,
                ActionIconSize: 18),
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
                CampaignRowHeight: 76,
                ActionIconSize: 16)
        };

        Set(application, "DungeonBaseFontSize", metrics.BaseFont);
        Set(application, "DungeonNavigationFontSize", metrics.NavigationFont);
        Set(application, "DungeonHelperFontSize", metrics.HelperFont);
        Set(application, "DungeonEyebrowFontSize", metrics.EyebrowFont);
        Set(application, "DungeonStatusFontSize", metrics.StatusFont);
        Set(application, "DungeonWorkspaceTitleFontSize", metrics.WorkspaceTitleFont);
        Set(application, "DungeonBrandMarkFontSize", metrics.BaseFont + 3);
        Set(application, "DungeonCampaignInitialFontSize", metrics.BaseFont + 2);
        Set(application, "DungeonEmptyMarkFontSize", metrics.BaseFont + 9);
        Set(application, "DungeonDetailInitialFontSize", metrics.BaseFont + 5);
        Set(application, "DungeonDetailTitleFontSize", metrics.BaseFont + 7);

        var sidebarWidth = sidebarVariant == SidebarVariant.Compact ? CompactSidebarWidth : metrics.SidebarWidth;
        Set(application, "DungeonTopBarHeight", new GridLength(metrics.TopBarHeight));
        Set(application, "DungeonSidebarWidth", new GridLength(sidebarWidth));
        Set(application, "DungeonStatusBarHeight", new GridLength(metrics.StatusBarHeight));
        Set(application, "DungeonWorkspaceHeaderHeight", metrics.WorkspaceHeaderHeight);
        Set(application, "DungeonControlHeight", metrics.ControlHeight);
        Set(application, "DungeonNavigationRowHeight", metrics.NavigationRowHeight);
        Set(application, "DungeonCampaignRowHeight", metrics.CampaignRowHeight);
        Set(application, "DungeonActionIconSize", metrics.ActionIconSize);
        Set(application, "DungeonTopBarActionSize", metrics.TopBarHeight);
        Set(application, "DungeonCampaignMasterWidth", new GridLength(profile switch
        {
            UiScaleProfile.Small => 720,
            UiScaleProfile.Large => 840,
            _ => 760
        }));
        Set(application, "DungeonMinimumWindowWidth", profile == UiScaleProfile.Large ? 1180d : 1024d);
        Set(application, "DungeonMinimumWindowHeight", profile == UiScaleProfile.Large ? 760d : 680d);
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
        double CampaignRowHeight,
        double ActionIconSize);
}
