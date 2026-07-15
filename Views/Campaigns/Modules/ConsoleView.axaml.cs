using Avalonia.Controls;

namespace DungeonApp.Views.Campaigns.Modules;

public partial class ConsoleView : UserControl
{
    public ConsoleView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is DungeonApp.Models.Campaigns.Engine.Modules.Core.ConsoleModule module && module.Feed != null)
        {
            // Opcjonalne odpięcie poprzedniego nasłuchiwania w przypadku zmiany DataContextu
            module.Feed.CollectionChanged -= OnFeedChanged;
            module.Feed.CollectionChanged += OnFeedChanged;
        }
    }

    private void OnFeedChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Wywołane, gdy kolekcja się zmienia. Avalonia potrzebuje chwili na wyrenderowanie.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ConsoleScroll.ScrollToEnd();
        }, Avalonia.Threading.DispatcherPriority.Background);
    }
}
