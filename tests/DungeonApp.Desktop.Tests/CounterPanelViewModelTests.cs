using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DungeonApp.Core.Campaigns;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Core.Tools.Counter;
using DungeonApp.Desktop.Features.CampaignWorkspace.Panels;
using DungeonApp.Desktop.Shell;

namespace DungeonApp.Desktop.Tests;

public sealed class CounterPanelViewModelTests
{
    [Fact]
    public void A_never_written_counter_is_shown_as_zero_and_can_change()
    {
        using var fixture = new Fixture();
        using var viewModel = fixture.CreateViewModel();

        Assert.Equal("0", viewModel.Count);
        Assert.True(viewModel.CanChange);
        Assert.False(viewModel.HasMessage);
        Assert.True(viewModel.HasReadableValue);
    }

    [Fact]
    public void An_external_write_of_the_counter_refreshes_the_view_model()
    {
        using var fixture = new Fixture();
        using var viewModel = fixture.CreateViewModel();
        var countNotifications = 0;
        viewModel.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName == nameof(CounterPanelViewModel.Count))
            {
                countNotifications++;
            }
        };

        fixture.Campaign.DataBlocks.Apply(
            CounterTool.DataBlockId,
            _ => new Dictionary<string, object> { [CounterTool.CountField] = -42L });

        Assert.Equal("-42", viewModel.Count);
        Assert.Equal(1, countNotifications);
    }

    [Fact]
    public async Task A_write_of_another_data_block_does_not_refresh_the_counter_state()
    {
        using var fixture = new Fixture();
        fixture.Campaign.DataBlocks.Apply(
            CounterTool.DataBlockId,
            _ => new Dictionary<string, object> { [CounterTool.CountField] = long.MaxValue });
        using var viewModel = fixture.CreateViewModel();

        await viewModel.IncrementCommand.ExecuteAsync();
        fixture.Campaign.DataBlocks.Apply(Fixture.OtherDataBlockId, _ => "changed");

        Assert.Equal(long.MaxValue.ToString(), viewModel.Count);
        Assert.Equal("Nie można zwiększyć licznika. Osiągnięto największą obsługiwaną wartość.", viewModel.Message);
    }

    [Fact]
    public async Task An_increment_uses_the_session_save_path_and_refreshes_after_the_data_block_event()
    {
        using var fixture = new Fixture();
        using var viewModel = fixture.CreateViewModel();

        await viewModel.IncrementCommand.ExecuteAsync();

        Assert.Equal("1", viewModel.Count);
        Assert.Equal(1, fixture.Repository.SaveCount);
    }

    [Fact]
    public async Task An_overflow_keeps_the_value_and_does_not_save()
    {
        using var fixture = new Fixture();
        fixture.Campaign.DataBlocks.Apply(
            CounterTool.DataBlockId,
            _ => new Dictionary<string, object> { [CounterTool.CountField] = long.MaxValue });
        using var viewModel = fixture.CreateViewModel();

        await viewModel.IncrementCommand.ExecuteAsync();

        Assert.Equal(long.MaxValue.ToString(), viewModel.Count);
        Assert.Equal("Nie można zwiększyć licznika. Osiągnięto największą obsługiwaną wartość.", viewModel.Message);
        Assert.Equal(0, fixture.Repository.SaveCount);
    }

    [Fact]
    public async Task A_disk_save_error_is_shown_after_the_in_memory_change()
    {
        using var fixture = new Fixture { SaveException = new IOException() };
        using var viewModel = fixture.CreateViewModel();

        await viewModel.IncrementCommand.ExecuteAsync();

        Assert.Equal("1", viewModel.Count);
        Assert.Equal("Zmiana nie została zapisana na dysku. Sprawdź dostęp do katalogu kampanii.", viewModel.Message);
        Assert.Equal(1, fixture.Repository.SaveCount);
    }

    [Fact]
    public void An_unreadable_counter_disables_both_actions()
    {
        using var fixture = new Fixture(unreadableCounter: true);
        using var viewModel = fixture.CreateViewModel();

        Assert.False(viewModel.CanChange);
        Assert.False(viewModel.IncrementCommand.CanExecute(null));
        Assert.False(viewModel.DecrementCommand.CanExecute(null));
        Assert.Equal("Nie można odczytać licznika.", viewModel.Message);
        Assert.False(viewModel.HasReadableValue);
    }

    [Fact]
    public async Task A_pending_save_blocks_the_opposite_command()
    {
        var repository = new WaitingRepository();
        using var fixture = new Fixture(repository);
        using var viewModel = fixture.CreateViewModel();

        var increment = viewModel.IncrementCommand.ExecuteAsync();
        await repository.WhenSaving;
        await viewModel.DecrementCommand.ExecuteAsync();

        Assert.Equal(1, repository.SaveCount);
        repository.CompleteSave();
        await increment;
    }

    [Fact]
    public void A_disposed_view_model_stops_commands_and_ignores_a_late_event_snapshot()
    {
        using var fixture = new Fixture();
        CounterPanelViewModel? viewModel = null;
        fixture.Campaign.Events.Subscribe<DataBlockChanged>(_ => viewModel!.Dispose());
        viewModel = fixture.CreateViewModel();

        fixture.Campaign.DataBlocks.Apply(
            CounterTool.DataBlockId,
            _ => new Dictionary<string, object> { [CounterTool.CountField] = 1L });

        Assert.False(viewModel.IncrementCommand.CanExecute(null));
        Assert.False(viewModel.DecrementCommand.CanExecute(null));
        Assert.Equal("0", viewModel.Count);
    }

    private sealed class Fixture : IDisposable
    {
        public static readonly DataBlockId OtherDataBlockId = DataBlockId.Create("test.other");

        private readonly DataBlockRegistry _registry = new DataBlockRegistry()
            .Register(CounterTool.DataBlockId, CounterTool.DataBlockVersion, CounterTool.DataBlockShape)
            .Register(OtherDataBlockId, 1, new PrimitiveShape(PrimitiveKind.Text));

        public Fixture(Repository? repository = null, bool unreadableCounter = false)
        {
            Campaign = unreadableCounter
                ? Campaign.Restore(
                    CampaignId.New(),
                    CampaignName.Create("Testowa"),
                    DateTimeOffset.UtcNow,
                    _registry,
                    new Dictionary<DataBlockId, object>(),
                    new Dictionary<DataBlockId, DataBlockUnreadableReason>
                    {
                        [CounterTool.DataBlockId] = DataBlockUnreadableReason.UnsupportedVersion,
                    })
                : Campaign.Create(CampaignName.Create("Testowa"), _registry, TimeProvider.System);
            Repository = repository ?? new Repository();
            Session = new CampaignSession(Campaign, Repository);
        }

        public Campaign Campaign { get; }

        public Repository Repository { get; }

        public CampaignSession Session { get; }

        public Exception? SaveException
        {
            get => Repository.Exception;
            set => Repository.Exception = value;
        }

        public CounterPanelViewModel CreateViewModel() => new(Session);

        public void Dispose()
        {
        }
    }

    private class Repository : ICampaignRepository
    {
        public int SaveCount { get; private set; }

        public Exception? Exception { get; set; }

        public virtual Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default)
        {
            RecordSave();
            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.CompletedTask;
        }

        protected void RecordSave() => SaveCount++;

        public Task<Campaign?> GetAsync(CampaignId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Campaign?>(null);

        public Task<IReadOnlyList<CampaignSummary>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CampaignSummary>>([]);
    }

    private sealed class WaitingRepository : Repository
    {
        private readonly TaskCompletionSource _save = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WhenSaving => _started.Task;

        public override Task SaveAsync(Campaign campaign, CancellationToken cancellationToken = default)
        {
            RecordSave();
            _started.SetResult();
            return _save.Task;
        }

        public void CompleteSave() => _save.SetResult();

    }
}
