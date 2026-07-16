using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using DungeonApp.Models.Campaigns.Engine.Events;
using DungeonApp.Models.Campaigns.Engine.Commands;
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
        
        var inputBox = this.FindControl<TextBox>("ConsoleInputBox");
        if (inputBox != null)
        {
            inputBox.AddHandler(Avalonia.Input.InputElement.KeyDownEvent, ConsoleInputBox_PreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            inputBox.PropertyChanged += InputBox_PropertyChanged;
        }
    }

    private Avalonia.Media.Typeface? _cachedTypeface;

    private void InputBox_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty && sender is TextBox tb)
        {
            if (_cachedTypeface == null)
            {
                _cachedTypeface = new Avalonia.Media.Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight);
            }
            UpdatePopupPosition(tb);
        }
    }

    private void UpdatePopupPosition(TextBox tb)
    {
        var listBox = this.FindControl<ListBox>("AutocompleteListBox");
        var module = this.DataContext as ConsoleModule;
        if (listBox == null || module == null) return;

        int tokenStart = module.CurrentTokenStart;
        string textUpToToken = tb.Text != null && tokenStart <= tb.Text.Length 
            ? tb.Text.Substring(0, tokenStart) 
            : string.Empty;

        // Obliczamy szerokość tekstu na bazie czcionki, by przesunąć wewnętrzny ListBox nad aktualny argument
        var formattedText = new Avalonia.Media.FormattedText(
            textUpToToken,
            System.Globalization.CultureInfo.CurrentCulture,
            Avalonia.Media.FlowDirection.LeftToRight,
            _cachedTypeface ?? Avalonia.Media.Typeface.Default,
            tb.FontSize,
            null
        );

        // +11 to lewy Padding TextBoxa (10) + grubość ramki (1), żeby tekst nakładał się precyzyjnie
        listBox.Margin = new Avalonia.Thickness(formattedText.Width + 11, 0, 0, 0);
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

    private void ConsoleInputBox_PreviewKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        var listBox = this.FindControl<ListBox>("AutocompleteListBox");
        var module = this.DataContext as ConsoleModule;
        if (listBox == null || module == null) return;
        
        if (e.Key == Avalonia.Input.Key.Escape)
        {
            if (module.IsAutocompleteOpen)
            {
                module.IsAutocompleteOpen = false;
                module.GhostText = string.Empty;
                e.Handled = true;
                return;
            }
        }

        if (!module.IsAutocompleteOpen) return;

        if (e.Key == Avalonia.Input.Key.Up)
        {
            MoveSelection(listBox, -1, module, sender as TextBox);
            e.Handled = true;
        }
        else if (e.Key == Avalonia.Input.Key.Down)
        {
            MoveSelection(listBox, +1, module, sender as TextBox);
            e.Handled = true;
        }
        else if (e.Key == Avalonia.Input.Key.Tab)
        {
            if (listBox.SelectedItem is string selectedStr)
            {
                CycleAutocompleteOption(listBox, module, selectedStr, sender as TextBox);
                e.Handled = true;
            }
        }
        // Enter — przepuszczamy dalej, ExecuteConsoleCommand wyczyści pole i zamknie Popup
    }

    private void CycleAutocompleteOption(ListBox listBox, ConsoleModule module, string selectedStr, TextBox? tb)
    {
        string currentToken = string.Empty;
        if (module.ConsoleInputText != null && module.ConsoleInputText.Length >= module.CurrentTokenStart)
        {
            currentToken = module.ConsoleInputText.Substring(module.CurrentTokenStart);
        }

        // Jeżeli wpisany token to DOKŁADNIE to samo co aktualnie podświetlona opcja
        if (currentToken == selectedStr)
        {
            // Uruchamiamy Tab-Cycling! (Przejście do następnej opcji)
            int nextIndex = (listBox.SelectedIndex + 1) % listBox.ItemCount;
            int attempts = 0;

            while (attempts < listBox.ItemCount && (listBox.Items[nextIndex] as string)?.StartsWith(CommandNode.HintPrefix) == true)
            {
                nextIndex = (nextIndex + 1) % listBox.ItemCount;
                attempts++;
            }

            if (nextIndex != listBox.SelectedIndex)
            {
                listBox.SelectedIndex = nextIndex;
                selectedStr = listBox.SelectedItem as string ?? selectedStr;
            }
        }

        module.ApplyAutocomplete(selectedStr);
                    
        // Przesuń kursor na sam koniec
        if (tb != null)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => tb.CaretIndex = tb.Text?.Length ?? 0);
        }
    }

    private void AutocompleteListBox_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var listBox = sender as ListBox;
        var module = this.DataContext as ConsoleModule;
        
        // Upewniamy się, że kliknięto w faktyczną opcję (np. TextBlock wewnątrz ListBoxItem), a nie tło ListBoxa
        if (e.Source is Avalonia.Controls.Control control && control.DataContext is string suggestion && module != null)
        {
            // Nie pozwól kliknąć w hinty (choć i tak blokuje to IsHitTestVisible, ale dla bezpieczeństwa)
            if (suggestion.StartsWith(CommandNode.HintPrefix)) return;

            module.ApplyAutocomplete(suggestion);
            
            // Przywróć focus do głównego pola tekstowego opóźnioną akcją
            var tb = this.FindControl<TextBox>("ConsoleInputBox");
            if (tb != null)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => 
                {
                    tb.Focus();
                    tb.CaretIndex = tb.Text?.Length ?? 0;
                });
            }
            e.Handled = true; // Zatrzymuje zdarzenie, więc TextBox z tyłu nie traci focusu i unikamy mignięcia!
        }
    }

    private void AutocompleteListBox_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var listBox = sender as ListBox;
        if (listBox == null) return;

        if (e.Source is Avalonia.Controls.Control control && control.DataContext is string suggestion)
        {
            // Nie zaznaczaj hintów (<<arg>>)
            if (suggestion.StartsWith(CommandNode.HintPrefix)) return;

            // Synchronizacja myszki z zaznaczeniem klawiaturowym. Zmiana SelectedItem powoduje
            // automatyczne odznaczenie poprzedniego elementu, więc żółty kolor zawsze "przeskakuje".
            if (listBox.SelectedItem as string != suggestion)
            {
                listBox.SelectedItem = suggestion;
            }
        }
    }

    /// <summary>
    /// Przesuwa selekcję w ListBoxie o podany krok, omijając "hinty" (np. &lt;&lt;x&gt;&gt;).
    /// </summary>
    private void MoveSelection(ListBox listBox, int direction, ConsoleModule? module, TextBox? textBox)
    {
        if (listBox.ItemCount == 0 || module == null) return;

        int startIndex = listBox.SelectedIndex;
        int next = startIndex;
        
        // Pętla do znalezienia najbliższej poprawnej podpowiedzi (omijanie hintów)
        for (int i = 0; i < listBox.ItemCount; i++)
        {
            next = direction > 0 
                ? (next + 1) % listBox.ItemCount 
                : (next - 1 + listBox.ItemCount) % listBox.ItemCount;

            if (listBox.Items[next] is string item && !item.StartsWith(CommandNode.HintPrefix))
            {
                break; // Znaleziono prawidłową opcję
            }
        }

        listBox.SelectedIndex = next;
        module.SelectedCommand = listBox.Items[next] as string; // Synchronizacja Modelu by binding nie wprowadzał opóźnień
        
        // TextBox.CaretIndex przesuwamy tylko opcjonalnie, tekst i ghost uaktualnia się samo przez bindowanie SelectedCommand
        if (textBox != null)
            Avalonia.Threading.Dispatcher.UIThread.Post(() => textBox.CaretIndex = textBox.Text?.Length ?? 0);
    }
    private void ConsoleInputBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var module = this.DataContext as ConsoleModule;
        if (module != null && module.IsAutocompleteOpen)
        {
            var tb = sender as TextBox;
            var listBox = this.FindControl<ListBox>("AutocompleteListBox");

            // Anti-Jumping Focus Fix: Jeśli mysz znajduje się nad listą autouzupełniania,
            // to powodem utraty fokusu jest kliknięcie w jedną z opcji (lub w tło overlay'u).
            // Przerywamy zamykanie popupa, aby nie ukrył się on przed przetworzeniem PointerPressed,
            // i profilaktycznie wpychamy focus z powrotem do TextBoxa.
            if (listBox != null && listBox.IsPointerOver)
            {
                if (tb != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => tb.Focus(), Avalonia.Threading.DispatcherPriority.Input);
                }
                return;
            }

            // Opóźniamy zamknięcie popupa. Jeśli kliknięto gdzie indziej, zamykamy gdy background task sprawdzi utratę.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (tb != null && !tb.IsFocused)
                {
                    module.IsAutocompleteOpen = false;
                    module.GhostText = string.Empty;
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }
}
