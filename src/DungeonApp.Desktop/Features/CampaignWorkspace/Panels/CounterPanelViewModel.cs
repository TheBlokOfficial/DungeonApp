using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DungeonApp.Core.DataBlocks;
using DungeonApp.Core.Tools.Counter;
using DungeonApp.Desktop.Shell;
using DungeonApp.Desktop.ViewModels;

namespace DungeonApp.Desktop.Features.CampaignWorkspace.Panels;

/// <summary>Stan okna licznika dla jednej otwartej kampanii.</summary>
public sealed class CounterPanelViewModel : ObservableObject, IDisposable
{
    private readonly CampaignSession _campaign;
    private readonly CounterTool _tool = new();
    private readonly IDisposable _subscription;

    private long _count;
    private bool _canChange;
    private bool _hasReadableValue;
    private bool _isBusy;
    private string? _message;
    private bool _isDisposed;

    public CounterPanelViewModel(CampaignSession campaign)
    {
        _campaign = campaign;
        _subscription = campaign.Campaign.Events.Subscribe<DataBlockChanged>(OnDataBlockChanged);

        IncrementCommand = new AsyncCommand(() => ChangeAsync(
            _tool.Increment(),
            "Nie można zwiększyć licznika. Osiągnięto największą obsługiwaną wartość."), CanChangeValue);
        DecrementCommand = new AsyncCommand(() => ChangeAsync(
            _tool.Decrement(),
            "Nie można zmniejszyć licznika. Osiągnięto najmniejszą obsługiwaną wartość."), CanChangeValue);

        Refresh();
    }

    public string Count => _count.ToString(CultureInfo.InvariantCulture);

    public bool CanChange
    {
        get => _canChange;
        private set
        {
            if (SetField(ref _canChange, value))
            {
                IncrementCommand.RaiseCanExecuteChanged();
                DecrementCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasReadableValue
    {
        get => _hasReadableValue;
        private set => SetField(ref _hasReadableValue, value);
    }

    public string? Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value))
            {
                RaisePropertyChanged(nameof(HasMessage));
            }
        }
    }

    public bool HasMessage => Message is not null;

    public AsyncCommand IncrementCommand { get; }

    public AsyncCommand DecrementCommand { get; }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _subscription.Dispose();
        IncrementCommand.RaiseCanExecuteChanged();
        DecrementCommand.RaiseCanExecuteChanged();
    }

    private async Task ChangeAsync(Func<object?, object> transform, string overflowMessage)
    {
        if (!CanChangeValue())
        {
            return;
        }

        IsBusy = true;
        try
        {
            Message = await _campaign.ExecuteAsync(() =>
                _campaign.Campaign.DataBlocks.Apply(CounterTool.DataBlockId, transform));
        }
        catch (OverflowException)
        {
            Message = overflowMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnDataBlockChanged(DataBlockChanged changed)
    {
        if (!_isDisposed && changed.Id == CounterTool.DataBlockId)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_campaign.Campaign.DataBlocks.IsUnreadable(CounterTool.DataBlockId))
        {
            CanChange = false;
            HasReadableValue = false;
            Message = "Nie można odczytać licznika.";
            return;
        }

        var value = _campaign.Campaign.DataBlocks.Read(CounterTool.DataBlockId);
        var fields = (IReadOnlyDictionary<string, object>?)value;
        var count = fields is null ? 0 : (long)fields[CounterTool.CountField];

        SetField(ref _count, count, nameof(Count));

        CanChange = true;
        HasReadableValue = true;
        Message = null;
    }

    private bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetField(ref _isBusy, value))
            {
                IncrementCommand.RaiseCanExecuteChanged();
                DecrementCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool CanChangeValue() => CanChange && !_isBusy && !_isDisposed;
}
