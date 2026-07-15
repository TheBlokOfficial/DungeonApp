using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;

namespace DungeonApp.Controls;

/// <summary>
/// Animowana lista logów konsoli wzorowana na Minecraft ChatAnimation mod.
/// Nowy log "wjeżdża" z dołu, a wszystkie istniejące płynnie przesuwają się ku górze.
/// </summary>
/// <remarks>
/// DLACZEGO Canvas zamiast StackPanel/ItemsControl:
/// Efekt "push-up" wymaga animowania pozycji każdego elementu indywidualnie
/// o wartość zależną od zmierzonej wysokości nowego wpisu (DesiredSize).
/// StackPanel/ItemsControl zarządzają pozycją dzieci automatycznie — nie można
/// przechwycić ich layoutu i animować go niezależnie. Canvas daje pełną kontrolę.
///
/// DLACZEGO Transitions zamiast Animation.RunAsync():
/// Animation.RunAsync() na TranslateTransform rzuca InvalidCastException — Avalonia
/// wewnętrznie castuje cel animacji do Visual, a TranslateTransform nim nie jest.
/// Transitions obserwują zmianę wartości i animują od starej do nowej automatycznie.
/// Canvas.Top ustawiamy raz na wartość finalną — animacja działa wyłącznie
/// na RenderTransform.Y i Opacity, które są czysto wizualne i nie kolidują z layoutem.
/// </remarks>
public partial class AnimatedFeedList : UserControl
{
    // ─── Parametry animacji ────────────────────────────────────────────────────

    /// <summary>Czas wejścia nowego elementu (slide-in + fade-in).</summary>
    private static readonly TimeSpan EntryDuration = TimeSpan.FromMilliseconds(200);

    /// <summary>Czas przesunięcia istniejących elementów ku górze (push-up).</summary>
    private static readonly TimeSpan PushDuration = TimeSpan.FromMilliseconds(180);

    /// <summary>Startowy offset Y nowego elementu (wjeżdża z tej odległości poniżej miejsca docelowego).</summary>
    private const double EntryOffsetY = 18.0;

    // ─── StyledProperty: ItemsSource ─────────────────────────────────────────

    /// <summary>
    /// Kolekcja logów do wyświetlenia. Nasłuchuje CollectionChanged.
    /// DLACZEGO IList zamiast ObservableCollection&lt;object&gt;:
    /// ObservableCollection&lt;CampaignEventBase&gt; nie jest kowariancją
    /// do ObservableCollection&lt;object&gt; — generyki w C# nie są kowariantne.
    /// IList + INotifyCollectionChanged pozwala przyjąć dowolną ObservableCollection.
    /// </summary>
    public static readonly StyledProperty<IList?> ItemsSourceProperty =
        AvaloniaProperty.Register<AnimatedFeedList, IList?>(nameof(ItemsSource));

    public IList? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    // ─── StyledProperty: FeedDataTemplate ────────────────────────────────────

    /// <summary>
    /// DataTemplate przekazywany z zewnątrz (np. z ConsoleView).
    /// Decyduje o tym, jak renderowany jest każdy typ zdarzenia (Notification vs Proposal).
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> FeedDataTemplateProperty =
        AvaloniaProperty.Register<AnimatedFeedList, IDataTemplate?>(nameof(FeedDataTemplate));

    public IDataTemplate? FeedDataTemplate
    {
        get => GetValue(FeedDataTemplateProperty);
        set => SetValue(FeedDataTemplateProperty, value);
    }

    // ─── Wewnętrzny stan ──────────────────────────────────────────────────────

    private Canvas? _canvas;

    /// <summary>Lista aktywnych elementów na Canvasie — od najstarszego do najnowszego.</summary>
    private readonly List<ContentPresenter> _presenters = new();

    /// <summary>
    /// Aktualna kolekcja, do której jesteśmy zasubskrybowani.
    /// DLACZEGO trzymamy referencję:
    /// Pozwala na idempotentne odpinanie — bez ryzyka podwójnej subskrypcji
    /// w przypadku, gdy OnPropertyChanged i OnLoaded oba wywołują Subscribe.
    /// </summary>
    private INotifyCollectionChanged? _subscribedCollection;

    public AnimatedFeedList()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _canvas = this.FindControl<Canvas>("FeedCanvas");

        // DLACZEGO Subscribe w OnLoaded zamiast OnPropertyChanged:
        // W momencie OnPropertyChanged Canvas może jeszcze nie być w drzewie wizualnym
        // (binding jest aplikowany przed OnLoaded). Subskrybujemy tutaj idempotentnie —
        // SubscribeToCollection sprawdza, czy kolekcja nie jest już podpięta.
        SubscribeToCollection(ItemsSource);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            UnsubscribeFromCurrent();
            SubscribeToCollection(change.NewValue as IList);
        }
    }

    private void SubscribeToCollection(IList? collection)
    {
        if (collection is not INotifyCollectionChanged notifiable) return;

        // DLACZEGO guard przed podwójną subskrypcją:
        // OnLoaded i OnPropertyChanged mogą oba zostać wywołane dla tej samej kolekcji
        // (OnPropertyChanged gdy binding ustawia wartość, OnLoaded po załadowaniu drzewa).
        // Bez guardu CollectionChanged byłby podpięty dwukrotnie → każdy nowy log
        // uruchamiałby AddItem dwa razy → dwa presentry dla jednego itemu = ghosting.
        if (ReferenceEquals(notifiable, _subscribedCollection)) return;

        UnsubscribeFromCurrent();
        notifiable.CollectionChanged += OnCollectionChanged;
        _subscribedCollection = notifiable;
    }

    private void UnsubscribeFromCurrent()
    {
        if (_subscribedCollection == null) return;
        _subscribedCollection.CollectionChanged -= OnCollectionChanged;
        _subscribedCollection = null;
    }

    // ─── Obsługa kolekcji ─────────────────────────────────────────────────────

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                var captured = item;
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () => AddItem(captured),
                    Avalonia.Threading.DispatcherPriority.Render);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                var captured = item;
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () => RemoveItem(captured),
                    Avalonia.Threading.DispatcherPriority.Render);
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(ClearAll, Avalonia.Threading.DispatcherPriority.Render);
        }
    }

    // ─── Dodawanie elementu (dwuetapowe) ─────────────────────────────────────

    /// <summary>
    /// Tworzy ContentPresenter dla nowego elementu i uruchamia animacje push-up + slide-in.
    /// </summary>
    /// <remarks>
    /// DLACZEGO dwuetapowy dispatch:
    /// DesiredSize po dodaniu do Canvas.Children jest (0,0) w tym samym cyklu dispatcha
    /// — Avalonia mierzy leniwie. Post z DispatcherPriority.Background uruchamia się
    /// po layout passie, gdy DesiredSize jest już poprawne.
    /// Alternatywa (ręczne Measure+Arrange) powoduje "ghosting": Canvas renderuje element
    /// w starym miejscu, a ręczne Arrange w nowym — element przez kilka ms jest w dwóch
    /// miejscach jednocześnie. Dwuetapowy dispatch tego unika całkowicie.
    /// </remarks>
    private void AddItem(object item)
    {
        if (_canvas == null) return;

        var presenter = new ContentPresenter
        {
            Content = item,
            ContentTemplate = FeedDataTemplate,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            Width = _canvas.Bounds.Width,
            // Niewidoczny od startu — Canvas mierzy go w tle bez żadnego flasha.
            // Opacity=0 ≠ IsVisible=false: element uczestniczy w layoutcie, DesiredSize jest obliczane.
            Opacity = 0,
        };

        // ── Etap 1: Dodaj poza ekranem — Canvas zmierzy go w następnym layout pass ──
        Canvas.SetLeft(presenter, 0);
        Canvas.SetTop(presenter, -9999);
        _canvas.Children.Add(presenter);

        // ── Etap 2: Po layout passie — odczytaj wymiary i uruchom animacje ──────
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (_canvas == null) return;

            var itemHeight = Math.Max(presenter.DesiredSize.Height, 24);
            var targetTop   = _canvas.Bounds.Height - itemHeight;

            // Push-up: ustaw Canvas.Top na finalną wartość, wyrównaj RenderTransform.Y,
            // animuj RenderTransform.Y → 0 (element wizualnie przesuwa się ku górze)
            foreach (var existing in _presenters)
            {
                var currentTop = Canvas.GetTop(existing);
                var newTop     = currentTop - itemHeight;
                Canvas.SetTop(existing, newTop);
                AnimatePushUp(existing, itemHeight);
            }

            // Slide-in: ustaw Canvas.Top na docelową pozycję (raz, bez późniejszych zmian),
            // animuj RenderTransform.Y od +EntryOffsetY do 0 + Opacity od 0 do 1
            Canvas.SetTop(presenter, targetTop);
            AnimateSlideIn(presenter, EntryOffsetY);

            _presenters.Add(presenter);
            CleanupOffscreenItems();

        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    private void RemoveItem(object item)
    {
        if (_canvas == null) return;
        var p = _presenters.Find(x => x.Content == item);
        if (p == null) return;
        _canvas.Children.Remove(p);
        _presenters.Remove(p);
    }

    private void ClearAll()
    {
        _canvas?.Children.Clear();
        _presenters.Clear();
    }

    // ─── Animacje ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Animuje istniejący element ku górze przez TranslateTransform.Y.
    /// Canvas.Top już wskazuje na pozycję finalną (ustawiony wcześniej) —
    /// TranslateTransform.Y kompensuje wizualnie i animuje do 0.
    /// </summary>
    private static void AnimatePushUp(ContentPresenter target, double fromY)
    {
        // Faza 1: ustaw transform startowy (bez Transition — instant, bez animacji)
        var transform = new TranslateTransform { Y = fromY };
        target.RenderTransform = transform;

        // Faza 2: w następnej klatce dodaj Transition i triggeruj do 0
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            transform.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = TranslateTransform.YProperty,
                    Duration  = PushDuration,
                    Easing    = new CubicEaseOut()
                }
            };
            transform.Y = 0;
        }, Avalonia.Threading.DispatcherPriority.Render);
    }

    /// <summary>
    /// Animuje nowy element: slide-in (TranslateTransform.Y) + fade-in (Opacity).
    /// Canvas.Top już wskazuje na pozycję finalną — animujemy tylko wizualny offset.
    /// </summary>
    private static void AnimateSlideIn(ContentPresenter target, double fromY)
    {
        // Faza 1: ustaw wartości startowe (bez Transitions)
        var transform = new TranslateTransform { Y = fromY };
        target.RenderTransform = transform;
        target.Opacity = 0;

        // Faza 2: dodaj Transitions i triggeruj do wartości docelowych
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            transform.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = TranslateTransform.YProperty,
                    Duration  = EntryDuration,
                    Easing    = new CubicEaseOut()
                }
            };

            target.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = Visual.OpacityProperty,
                    Duration  = EntryDuration,
                    Easing    = new CubicEaseOut()
                }
            };

            transform.Y = 0;
            target.Opacity = 1.0;
        }, Avalonia.Threading.DispatcherPriority.Render);
    }

    // ─── Garbage Collection ────────────────────────────────────────────────────

    /// <summary>
    /// Usuwa z Canvasa elementy, które całkowicie wyszły poza górną krawędź.
    /// Wywołana po każdym nowym wpisie.
    /// </summary>
    private void CleanupOffscreenItems()
    {
        if (_canvas == null) return;

        var toRemove = new List<ContentPresenter>();
        foreach (var p in _presenters)
        {
            if (Canvas.GetTop(p) + p.DesiredSize.Height < 0)
                toRemove.Add(p);
        }

        foreach (var p in toRemove)
        {
            _canvas.Children.Remove(p);
            _presenters.Remove(p);
        }
    }
}
