using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;

namespace DungeonApp.Desktop.Themes;

/// <summary>
/// Otwarcie okienka wysuwanego i rozwiniętej listy (każde okno wyskakujące, którego treścią jest
/// FlyoutPresenter: Flyout, ComboBox, DropDownPicker), raz dla całej aplikacji
/// (docs/architecture.md, "Niezmiennik interfejsu", Okienko i lista rozwijana):
/// <list type="bullet">
/// <item>ustala, czy okno stanęło pod otwierającym, czy nad nim, i daje otwierającemu klasę
/// <see cref="OpensUpClass"/>, gdy nad - motyw obraca po niej strzałkę listy w górę;</item>
/// <item>wyłania powierzchnię z przezroczystości i dosuwa ją o DungeonPopupOpenOffset od strony
/// otwierającego w DungeonPopupOpenDuration. Okno jest otwarte i klikalne od pierwszej klatki -
/// ruch tylko dogania stan. Zamknięcie jest natychmiastowe (nic tu go nie dotyczy).</item>
/// </list>
/// Wyłączone animacje w systemie: ReducedMotion.axaml ustawia <see cref="IsEnabledProperty"/>
/// na false i ruch się nie uruchamia; kierunek strzałki działa dalej.
/// </summary>
/// <remarks>
/// Obsługa klasowa zmiany Popup.IsOpen biegnie po otwarciu okna, więc jego położenie na ekranie
/// jest już ustalone. Zmienia wyłącznie wygląd widoku, nigdy stan aplikacji.
/// </remarks>
internal static class PopupOpenMotion
{
    public const string OpensUpClass = "opens-up";

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<FlyoutPresenter, bool>("IsEnabled", typeof(PopupOpenMotion), defaultValue: true);

    private static bool registered;

    public static bool GetIsEnabled(FlyoutPresenter presenter) => presenter.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(FlyoutPresenter presenter, bool value) => presenter.SetValue(IsEnabledProperty, value);

    public static void Register()
    {
        if (registered)
        {
            return;
        }

        registered = true;
        Popup.IsOpenProperty.Changed.AddClassHandler<Popup>(OnIsOpenChanged);
    }

    private static void OnIsOpenChanged(Popup popup, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true || popup.Child is not FlyoutPresenter presenter)
        {
            return;
        }

        var target = popup.PlacementTarget ?? popup.Parent as Control;
        var popupTop = presenter.PointToScreen(default).Y;
        var targetTop = target?.PointToScreen(default).Y;
        var opensUp = targetTop is { } top && popupTop < top;

        // Otwierający: kontrolka, której szablon zawiera okno (ComboBox, DropDownPicker), albo cel
        // okienka wysuwanego (przycisk).
        var opener = popup.TemplatedParent as Control ?? target;
        opener?.Classes.Set(OpensUpClass, opensUp);

        if (GetIsEnabled(presenter))
        {
            Run(presenter, opensUp);
        }
    }

    private static void Run(FlyoutPresenter presenter, bool opensUp)
    {
        var duration = presenter.TryFindResource("DungeonPopupOpenDuration", out var d) && d is TimeSpan span
            ? span
            : TimeSpan.FromMilliseconds(120);
        var offset = presenter.TryFindResource("DungeonPopupOpenOffset", out var o) && o is double value ? value : 4d;

        // Animacja przesunięcia działa na TranslateTransform w RenderTransform (animator przekształceń
        // Avalonii 12 nie animuje RenderTransform zapisanego jako TransformOperations).
        if (presenter.RenderTransform is not TranslateTransform)
        {
            presenter.RenderTransform = new TranslateTransform();
        }

        // Otwarte w dół - z góry (ujemne przesunięcie), otwarte w górę - z dołu.
        var from = opensUp ? offset : -offset;
        var animation = new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0d),
                        new Setter(TranslateTransform.YProperty, from),
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1d),
                        new Setter(TranslateTransform.YProperty, 0d),
                    },
                },
            },
        };

        _ = animation.RunAsync(presenter);
    }
}
