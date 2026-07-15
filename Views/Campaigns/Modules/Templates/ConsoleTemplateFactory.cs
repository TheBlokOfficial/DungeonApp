using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DungeonApp.Models.Campaigns.Engine.Events;
using DungeonApp.Models.Campaigns.Engine.Modules.Core;

namespace DungeonApp.Views.Campaigns.Modules.Templates;

/// <summary>
/// Fabryka widoków dla typów zdarzeń wyświetlanych w konsoli kampanii.
/// Buduje drzewa kontrolek programatycznie, co pozwala na przekazanie
/// ich do FuncDataTemplate w AnimatedFeedList.
/// </summary>
/// <remarks>
/// DLACZEGO fabryka C# zamiast DataTemplate w XAML:
/// FuncDataTemplate wymaga zwrócenia Control, a nie DataTemplate ze TemplateContent.
/// Przeniesienie logiki budowania widoków do C# zachowuje pełną kontrolę
/// nad strukturą i pozwala na łatwe dodawanie nowych typów eventów w przyszłości.
///
/// DLACZEGO nie ViewModel dla przycisków akceptacji/odrzucenia:
/// Propozycje są krótkotrwałymi elementami konsoli z lambdami AcceptAction/RejectAction.
/// Tworzenie ViewModelu dla każdej propozycji byłoby nadmiarowe. Bezpośrednie
/// podpięcie kliknięcia do lambdy w modelu zdarzenia jest wystarczające i czytelne.
/// </remarks>
public static class ConsoleTemplateFactory
{
    /// <summary>
    /// Buduje widok dla NotificationEvent: [INFO] [ModuleId] Wiadomość
    /// </summary>
    public static Control BuildNotificationView(NotificationEvent notification)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Avalonia.Thickness(0, 3)
        };

        panel.Children.Add(new TextBlock
        {
            Text = "[INFO]",
            Foreground = Brushes.DimGray,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"[{notification.SenderModuleId}]",
            Foreground = Brushes.DimGray,
            VerticalAlignment = VerticalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = notification.Message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });

        return panel;
    }

    /// <summary>
    /// Buduje widok dla ProposalEvent: [PROPOSAL] [ModuleId] Opis [accept] [reject]
    /// </summary>
    /// <param name="proposal">Zdarzenie propozycji.</param>
    /// <param name="consoleView">
    /// Referencja do widoku konsoli — potrzebna do resolwowania DataContext (ConsoleModule)
    /// dla komend AcceptProposal/RejectProposal.
    /// </param>
    public static Control BuildProposalView(ProposalEvent proposal, ConsoleView consoleView)
    {
        var panel = new WrapPanel { Margin = new Avalonia.Thickness(0, 5) };

        panel.Children.Add(new TextBlock
        {
            Text = "[PROPOSAL] ",
            Foreground = new SolidColorBrush(Color.Parse("#C9A84C")), // AccentGold
            FontWeight = FontWeight.Bold
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"[{proposal.SenderModuleId}] ",
            Foreground = Brushes.DimGray
        });

        panel.Children.Add(new TextBlock
        {
            Text = proposal.Description + " ",
            TextWrapping = TextWrapping.Wrap
        });

        var acceptBtn = new Button
        {
            Content = "[accept]",
            Background = Brushes.Transparent,
            Foreground = Brushes.LimeGreen,
            Padding = new Avalonia.Thickness(0),
            Margin = new Avalonia.Thickness(0, 0, 5, 0),
            Cursor = new Cursor(StandardCursorType.Hand),
            BorderThickness = new Avalonia.Thickness(0)
        };

        var rejectBtn = new Button
        {
            Content = "[reject]",
            Background = Brushes.Transparent,
            Foreground = Brushes.IndianRed,
            Padding = new Avalonia.Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand),
            BorderThickness = new Avalonia.Thickness(0)
        };

        // Podpinamy kliknięcia bezpośrednio do komend w ConsoleModule
        acceptBtn.Click += (_, _) =>
        {
            if (consoleView.DataContext is ConsoleModule module)
                module.AcceptProposalCommand.Execute(proposal);
        };

        rejectBtn.Click += (_, _) =>
        {
            if (consoleView.DataContext is ConsoleModule module)
                module.RejectProposalCommand.Execute(proposal);
        };

        panel.Children.Add(acceptBtn);
        panel.Children.Add(rejectBtn);

        return panel;
    }
}
