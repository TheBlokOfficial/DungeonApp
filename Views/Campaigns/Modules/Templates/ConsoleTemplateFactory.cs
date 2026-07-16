using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Documents;
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
        if (notification.SenderModuleId == "DM")
        {
            var textBlock = new SelectableTextBlock 
            { 
                TextWrapping = TextWrapping.Wrap, 
                Margin = new Avalonia.Thickness(0, 3) 
            };

            textBlock.Inlines?.Add(new Run { Text = "> ", Foreground = Brushes.DimGray, FontWeight = FontWeight.Bold });
            textBlock.Inlines?.Add(new Run { Text = notification.Message, Foreground = Brushes.LightGray });

            return textBlock;
        }
        else
        {
            var textBlock = new SelectableTextBlock 
            { 
                TextWrapping = TextWrapping.Wrap, 
                Margin = new Avalonia.Thickness(0, 3) 
            };

            // Komunikat Systemowy / Z modułu
            string levelText = notification.Level switch
            {
                "Warning" => "[WARN] ",
                "Error" => "[ERROR] ",
                _ => "[INFO] "
            };

            IBrush levelBrush = notification.Level switch
            {
                "Warning" => Brushes.Goldenrod,
                "Error" => Brushes.IndianRed,
                _ => Brushes.DimGray
            };

            textBlock.Inlines?.Add(new Run { Text = levelText, Foreground = levelBrush, FontWeight = FontWeight.Bold });
            textBlock.Inlines?.Add(new Run { Text = $"[{TranslateModuleName(notification.SenderModuleId)}] ", Foreground = Brushes.DimGray });
            textBlock.Inlines?.Add(new Run { Text = notification.Message });

            return textBlock;
        }
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

        panel.Children.Add(new SelectableTextBlock
        {
            Text = "[PROPOSAL] ",
            Foreground = new SolidColorBrush(Color.Parse("#C9A84C")), // AccentGold
            FontWeight = FontWeight.Bold
        });

        panel.Children.Add(new SelectableTextBlock
        {
            Text = $"[{TranslateModuleName(proposal.SenderModuleId)}] ",
            Foreground = Brushes.DimGray
        });

        panel.Children.Add(new SelectableTextBlock
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

    private static string TranslateModuleName(string moduleId)
    {
        if (moduleId == "DM") return "DM";
        
        string key = moduleId switch
        {
            "Core.Timekeeper" => "module_name_timekeeper",
            "Core.Console" => "module_name_console",
            _ => moduleId
        };
        
        var ts = App.Current?.Services?.GetService(typeof(DungeonApp.Services.ITranslationService)) as DungeonApp.Services.ITranslationService;
        return ts?.Translate(key) ?? key;
    }
}
