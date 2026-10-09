using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DungeonApp.Core.Campaigns;
using DungeonApp.Desktop.Diagnostics;
using DungeonApp.Desktop.Features.CampaignLibrary;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.Startup;
using DungeonApp.Desktop.Systems;
using DungeonApp.Desktop.Workspace.Layout;
using DungeonApp.Testing;

namespace DungeonApp.Desktop.RenderingTests;

/// <summary>
/// A startup step that throws must neither block entry to the program nor vanish without a trace:
/// the shell becomes ready with the step's own warning, and the exception reaches the log.
/// </summary>
public sealed class StartupFailureTests
{
    [AvaloniaFact]
    public async Task A_failing_step_finishes_startup_with_its_warning_and_logs_the_exception()
    {
        var failure = new InvalidOperationException("krok zawiódł");
        var logged = new List<(LogLevel Level, Exception? Exception)>();
        var previous = AppLog.Use((level, _, exception) =>
        {
            lock (logged)
            {
                logged.Add((level, exception));
            }
        });

        try
        {
            var shell = BuildShell(new FailingStep(failure));
            var host = new ContentControl { Content = new Panel() };

            await shell.RunStartupAsync(new StartupUiContext(host));

            Assert.True(shell.IsReady);
            Assert.Equal(FailingStep.Warning, shell.StatusBar.Message);
            Assert.Null(host.Content);
            lock (logged)
            {
                Assert.Contains(logged, entry => entry.Level == LogLevel.Error && ReferenceEquals(entry.Exception, failure));
            }
        }
        finally
        {
            AppLog.Use(previous);
        }
    }

    private static AppShellViewModel BuildShell(IStartupStep step)
    {
        IReadOnlyList<IGameSystem> systems = [new EmptyGameSystem()];
        ICampaignRepository repository = new InMemoryCampaignRepository();
        var repositoriesBySystem = systems.ToDictionary(system => system.Id, _ => repository);
        var preparations = new CampaignPreparationCache(repositoriesBySystem, systems);
        var campaignLibrary = new CampaignLibraryViewModel(
            repositoriesBySystem, new CreateCampaign(repositoriesBySystem, TimeProvider.System), preparations, systems, _ => Task.CompletedTask);

        var layoutStore = new WorkspaceLayoutStore(Path.Combine(Path.GetTempPath(), $"DungeonApp-startup-tests-{Guid.NewGuid():N}"));

        return new AppShellViewModel(systems, repositoriesBySystem, campaignLibrary, preparations, layoutStore, [step]);
    }

    private sealed class FailingStep(Exception failure) : IStartupStep
    {
        public const string Warning = "Krok testowy zawiódł.";

        public string Describe() => "Krok testowy";

        public Task PrepareAsync(CancellationToken cancellationToken) => Task.FromException(failure);

        public Task ApplyAsync(StartupUiContext ui, CancellationToken cancellationToken) => Task.CompletedTask;

        public string FailureWarning => Warning;
    }
}
