using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using DungeonApp.Desktop.Controls;

namespace DungeonApp.Desktop.Shell.Gallery.Sections;

public partial class FeedbackSection : UserControl
{
    public FeedbackSection()
    {
        InitializeComponent();
        StaticPalette.Configure(CreateSamplePalette(item => PaletteAnswer.Text = "Wybrano: " + item.Name));
    }

    private static readonly PaletteItem[] SampleItems =
    [
        new("chest", "DungeonIconBookOpen", "Skrzynia", "pojemnik, drewno", "Rzadkie", Brushes.Goldenrod),
        new("guard", "DungeonIconBookOpen", "Strażnik", "humanoid, straż", "1/8"),
        new("guardian", "DungeonIconBookOpen", "Strażnik bramy", "konstrukt", "3"),
        new("key", "DungeonIconBookOpen", "Klucz", "drobiazg"),
    ];

    private static PaletteOptions CreateSamplePalette(Action<PaletteItem> chosen) => new()
    {
        TargetText = "Dodaj do: Jaskinia",
        Placeholder = "Szukaj…",
        Hint = "Liczba przed nazwą dodaje kilka sztuk: 4 str",
        AllowQuantity = true,
        AllowKeepOpen = true,
        Search = query => SampleItems
            .Where(i => i.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .ToList(),
        Choose = (item, quantity) =>
        {
            chosen(item with { Name = quantity == 1 ? item.Name : item.Name + " ×" + quantity });
            return Task.CompletedTask;
        },
    };

    private async void OnPaletteClick(object? sender, RoutedEventArgs e) =>
        await Palette.ShowAsync((Button)sender!, CreateSamplePalette(item => PaletteAnswer.Text = "Wybrano: " + item.Name));

    private async void OnConfirmMainClick(object? sender, RoutedEventArgs e)
    {
        var confirmed = await ConfirmationDialog.ShowAsync(
            (Button)sender!,
            "Zapisać zmiany?",
            "Kampania ma niezapisane zmiany. Zapis nadpisze plik kampanii na dysku.",
            "Zapisz",
            isDestructive: false);
        ShowAnswer(confirmed);
    }

    private async void OnConfirmDestructiveClick(object? sender, RoutedEventArgs e)
    {
        var confirmed = await ConfirmationDialog.ShowAsync(
            (Button)sender!,
            "Usunąć kampanię?",
            "Kampania „Kopalnia Phandelver” zniknie z biblioteki razem z plikiem. Tego nie da się cofnąć.",
            "Usuń",
            isDestructive: true);
        ShowAnswer(confirmed);
    }

    private void ShowAnswer(bool confirmed) => ConfirmationAnswer.Text = confirmed ? "Potwierdzono" : "Anulowano";

    private void OnInformationClick(object? sender, RoutedEventArgs e) =>
        NotificationToast.Show(this, NotificationKind.Information, "Zapisano kampanię.");

    private void OnWarningClick(object? sender, RoutedEventArgs e) =>
        NotificationToast.Show(this, NotificationKind.Warning, "Paczka „Bestiariusz” ma wpisy bez nazwy - pominięto 3.");

    private void OnErrorClick(object? sender, RoutedEventArgs e) =>
        NotificationToast.Show(this, NotificationKind.Error, "Nie udało się zapisać kampanii: brak dostępu do pliku C:\\Kampanie\\phandelver.dungeon.");

    private void OnWithActionClick(object? sender, RoutedEventArgs e) =>
        NotificationToast.Show(
            this,
            NotificationKind.Information,
            "Usunięto wpis „Goblin”.",
            "Szczegóły",
            () => ConfirmationAnswer.Text = "Kliknięto akcję powiadomienia");

    private void OnLongClick(object? sender, RoutedEventArgs e) =>
        NotificationToast.Show(
            this,
            NotificationKind.Warning,
            "Bardzo długa treść powiadomienia: dymek ma stałą szerokość, więc tekst zawija się w kolejne linie, "
            + "a dymek rośnie w pionie. Nic nie rozpycha go w bok ani nie przycina treści wielokropkiem.",
            "Otwórz ustawienia",
            () => ConfirmationAnswer.Text = "Kliknięto akcję powiadomienia");
}
