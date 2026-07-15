using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DungeonApp.Models.Campaigns.Engine.Events;
using DungeonApp.Controls;
using DungeonApp.Models.Campaigns.Engine.Modules.Core;
using DungeonApp.Views.Campaigns.Modules.Templates;

namespace DungeonApp.Views.Campaigns.Modules;

/// <summary>
/// Code-behind dla widoku konsoli kampanii.
/// </summary>
/// <remarks>
/// DLACZEGO FeedDataTemplate ustawiany z code-behind (nie z XAML):
/// AnimatedFeedList renderuje elementy przez ContentPresenter i potrzebuje jednego
/// IDataTemplate do wyboru szablonu per-typ. W XAML nie ma składni "DataTemplateSelector"
/// jak w WPF. FuncDataTemplate (Avalonia) pozwala zaimplementować ten mechanizm
/// w C# — sprawdza typ i zwraca odpowiedni Control. To standard w projektach Avalonia.
///
/// DLACZEGO usunięto stary OnFeedChanged + ScrollToEnd:
/// AnimatedFeedList zarządza scrollem i pozycją wewnętrznie. Zewnętrzne wywołanie
/// ScrollToEnd nie jest już potrzebne ani możliwe (nie ma ScrollViewera w drzewie).
/// </remarks>
public partial class ConsoleView : UserControl
{
    public ConsoleView()
    {
        InitializeComponent();
        SetupFeedTemplate();
    }

    /// <summary>
    /// Ustawia FuncDataTemplate na AnimatedFeedList, które dynamicznie
    /// dobiera widok do wyświetlenia w zależności od typu zdarzenia.
    /// </summary>
    private void SetupFeedTemplate()
    {
        var feed = this.FindControl<AnimatedFeedList>("ConsoleFeed");
        if (feed == null) return;

        feed.FeedDataTemplate = new FuncDataTemplate<object>((item, _) =>
        {
            if (item is ProposalEvent proposal)
                return ConsoleTemplateFactory.BuildProposalView(proposal, this);

            if (item is NotificationEvent notification)
                return ConsoleTemplateFactory.BuildNotificationView(notification);

            return new TextBlock { Text = item?.ToString() ?? string.Empty };
        });
    }
}
