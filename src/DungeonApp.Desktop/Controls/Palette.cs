using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DungeonApp.Desktop.Diagnostics;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Palette: small search window at the top centre of the application window, above everything
/// (hosted in <see cref="WindowOverlay"/>). A dimmed target line, a search field, an optional hint and a list of
/// results the caller supplies through <see cref="PaletteOptions.Search"/> - the palette knows nothing about what
/// the results are. Up/Down move the selection (focus stays in the field), Enter chooses and closes,
/// Ctrl+Enter (when allowed) chooses and keeps it open, Escape or a click outside closes, a click on a row is
/// Enter. One palette at a time: opening another closes the current one. Appearance belongs to the shell
/// theme (Themes/Controls/Palette.axaml).
/// <para>
/// Glass: on opening the palette takes a snapshot of the window under it (<see cref="BackdropCapture"/>) and its
/// card shows it blurred (<see cref="GlassBackdrop"/>, pseudo-class <c>:glass</c>). The snapshot stays true while
/// the palette is open, because the palette captures input and the workspace changes only after a GM action.
/// It is released on closing. A failed snapshot leaves the plain card and an <see cref="AppLog"/> entry.
/// </para>
/// </summary>
[TemplatePart(SearchPart, typeof(TextBox))]
[TemplatePart(RowsPart, typeof(ItemsControl))]
[TemplatePart(CardPart, typeof(Control))]
[TemplatePart(EmptyPart, typeof(Control))]
[TemplatePart(BackdropPart, typeof(GlassBackdrop))]
[PseudoClasses(GlassPseudoClass)]
public sealed class Palette : TemplatedControl
{
    public const string SearchPart = "PART_Search";
    public const string RowsPart = "PART_Rows";
    public const string CardPart = "PART_Card";
    public const string EmptyPart = "PART_Empty";
    public const string BackdropPart = "PART_Backdrop";
    public const string GlassPseudoClass = ":glass";

    public static readonly StyledProperty<string?> TargetTextProperty =
        AvaloniaProperty.Register<Palette, string?>(nameof(TargetText));

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<Palette, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<Palette, string?>(nameof(Hint));

    private static Palette? current;

    private readonly ObservableCollection<PaletteRowModel> rows = new();
    private TaskCompletionSource? closed;
    private PaletteOptions? options;
    private TextBox? search;
    private ItemsControl? rowsControl;
    private Control? card;
    private Control? empty;
    private GlassBackdrop? backdropControl;
    private Bitmap? backdrop;
    private WindowOverlay? host;
    private IInputElement? returnFocusTo;
    private int selectedIndex = -1;
    private bool focusSearchOnLoad;

    public Palette()
    {
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
        AddHandler(Button.ClickEvent, OnRowClick);
    }

    /// <summary>Resolves the main window when the opening element sits in a pop-up (context menu) with no overlay.</summary>
    internal static Func<Window?> MainWindowProvider { get; set; } = () =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    /// <summary>Takes the snapshot of the window under the overlay shown through the glass; replaceable in tests.</summary>
    internal static Func<WindowOverlay, Bitmap?> BackdropCapture { get; set; } = CaptureBehind;

    internal Bitmap? Backdrop => backdrop;

    public string? TargetText
    {
        get => GetValue(TargetTextProperty);
        set => SetValue(TargetTextProperty, value);
    }

    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    internal ObservableCollection<PaletteRowModel> Rows => rows;

    internal int SelectedIndex => selectedIndex;

    /// <summary>
    /// Opens a palette over the window containing <paramref name="origin"/> (or over the main window when the
    /// origin lives in a pop-up); completes when the palette closes. An open palette is closed first.
    /// </summary>
    public static Task ShowAsync(Visual origin, PaletteOptions options)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(options);

        var overlay = FindOverlay(origin)
            ?? throw new InvalidOperationException("Okno elementu nie ma warstwy WindowOverlay (MainWindow.axaml).");

        current?.Close();

        var palette = new Palette { focusSearchOnLoad = true };
        palette.Configure(options);
        palette.host = overlay;
        palette.returnFocusTo = TopLevel.GetTopLevel(origin)?.FocusManager?.GetFocusedElement();
        palette.closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        palette.SetBackdrop(TryCaptureBackdrop(overlay));
        current = palette;
        overlay.Children.Add(palette);
        return palette.closed.Task;
    }

    /// <summary>Sets what the palette shows and does, and fills the list for the empty query.</summary>
    public void Configure(PaletteOptions newOptions)
    {
        ArgumentNullException.ThrowIfNull(newOptions);
        options = newOptions;
        TargetText = newOptions.TargetText;
        Placeholder = newOptions.Placeholder;
        Hint = newOptions.Hint;
        Refresh();
    }

    internal static WindowOverlay? FindOverlay(Visual origin) =>
        WindowOverlay.Find(origin) ?? (MainWindowProvider() is { } main ? WindowOverlay.Find(main) : null);

    private static Bitmap? TryCaptureBackdrop(WindowOverlay overlay)
    {
        try
        {
            var bitmap = BackdropCapture(overlay);
            if (bitmap is null)
            {
                AppLog.Error("Paleta: brak zdjęcia okna pod paletą, tło bez szkła.");
            }

            return bitmap;
        }
        catch (Exception exception)
        {
            AppLog.Error("Paleta: zdjęcie okna pod paletą nie powiodło się, tło bez szkła.", exception);
            return null;
        }
    }

    // The whole window at its pixel density, without the overlay layer: the palette and toasts above the
    // content are not part of what the glass shows.
    private static Bitmap? CaptureBehind(WindowOverlay overlay)
    {
        if (TopLevel.GetTopLevel(overlay) is not { } top || top.Bounds.Width <= 0 || top.Bounds.Height <= 0)
        {
            return null;
        }

        var scale = top.RenderScaling;
        var size = new PixelSize(
            (int)Math.Ceiling(top.Bounds.Width * scale),
            (int)Math.Ceiling(top.Bounds.Height * scale));
        var bitmap = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale));
        var opacity = overlay.Opacity;
        try
        {
            overlay.Opacity = 0;
            bitmap.Render(top);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
        finally
        {
            overlay.Opacity = opacity;
        }
    }

    private void SetBackdrop(Bitmap? bitmap)
    {
        backdrop = bitmap;
        PseudoClasses.Set(GlassPseudoClass, bitmap is not null);
        if (backdropControl is not null)
        {
            backdropControl.Source = bitmap;
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (search is not null)
        {
            search.TextChanged -= OnSearchChanged;
        }

        search = e.NameScope.Find<TextBox>(SearchPart);
        rowsControl = e.NameScope.Find<ItemsControl>(RowsPart);
        card = e.NameScope.Find<Control>(CardPart);
        empty = e.NameScope.Find<Control>(EmptyPart);
        backdropControl = e.NameScope.Find<GlassBackdrop>(BackdropPart);
        if (backdropControl is not null)
        {
            backdropControl.Source = backdrop;
        }

        if (search is not null)
        {
            search.TextChanged += OnSearchChanged;
        }

        if (rowsControl is not null)
        {
            rowsControl.ItemsSource = rows;
        }

        UpdateEmpty();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (options is not null)
        {
            Refresh();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (!focusSearchOnLoad)
        {
            return;
        }

        search?.Focus(NavigationMethod.Tab);
        var offset = this.TryFindResource("DungeonPaletteOpenOffset", out var o) && o is double value ? value : 14d;
        OverlayAppearMotion.Run(this, card ?? this, 0d, -offset, "DungeonPaletteOpenDuration");
        HoldBackdropStill();
    }

    // The card slides in with a transform; the snapshot inside it moves the opposite way, so the blurred window
    // stays aligned with the window behind it while the card's outline travels over it.
    private void HoldBackdropStill()
    {
        if (card?.RenderTransform is not TranslateTransform slide || backdropControl is null)
        {
            return;
        }

        var counter = new TranslateTransform(-slide.X, -slide.Y);
        backdropControl.RenderTransform = counter;
        slide.PropertyChanged += (_, change) =>
        {
            if (change.Property == TranslateTransform.XProperty)
            {
                counter.X = -slide.X;
            }
            else if (change.Property == TranslateTransform.YProperty)
            {
                counter.Y = -slide.Y;
            }
        };
    }

    // A click outside the card closes; a click inside is left to its controls.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (host is null)
        {
            return;
        }

        var inside = card is not null && e.Source is Visual source && (source == card || card.IsVisualAncestorOf(source));
        if (!inside)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        if (options is null)
        {
            return;
        }

        var (quantity, query) = PaletteQuery.Parse(search?.Text, options.AllowQuantity);
        var results = options.Search(query);

        rows.Clear();
        foreach (var item in results)
        {
            rows.Add(new PaletteRowModel(item, quantity, ResolveIcon(item.IconKey)));
        }

        SelectIndex(rows.Count > 0 ? 0 : -1);
        UpdateEmpty();
    }

    private Avalonia.Media.DrawingImage? ResolveIcon(string key)
    {
        if (this.TryFindResource(key, out var value) || (Application.Current?.TryFindResource(key, out value) ?? false))
        {
            return value as DrawingImage;
        }

        return null;
    }

    private void UpdateEmpty()
    {
        if (empty is not null)
        {
            empty.IsVisible = rows.Count == 0;
        }
    }

    private void SelectIndex(int index)
    {
        if (selectedIndex >= 0 && selectedIndex < rows.Count)
        {
            rows[selectedIndex].IsSelected = false;
        }

        selectedIndex = index;
        if (index < 0 || index >= rows.Count)
        {
            selectedIndex = -1;
            return;
        }

        rows[index].IsSelected = true;
        Dispatcher.UIThread.Post(() => rowsControl?.ContainerFromIndex(index)?.BringIntoView(), DispatcherPriority.Loaded);
    }

    private void Move(int delta)
    {
        if (rows.Count == 0)
        {
            return;
        }

        SelectIndex(Math.Clamp(selectedIndex + delta, 0, rows.Count - 1));
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                e.Handled = true;
                Close();
                break;
            case Key.Down:
                e.Handled = true;
                Move(1);
                break;
            case Key.Up:
                e.Handled = true;
                Move(-1);
                break;
            case Key.Enter:
                e.Handled = true;
                Choose(keepOpen: e.KeyModifiers.HasFlag(KeyModifiers.Control) && options?.AllowKeepOpen == true);
                break;
        }
    }

    private void OnRowClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control { DataContext: PaletteRowModel model })
        {
            var index = rows.IndexOf(model);
            if (index >= 0)
            {
                SelectIndex(index);
                Choose(keepOpen: false);
                e.Handled = true;
            }
        }
    }

    // Async void on purpose: an exception from the caller's action goes to the UI thread handler instead of
    // being swallowed here.
    private async void Choose(bool keepOpen)
    {
        if (options is null || selectedIndex < 0)
        {
            return;
        }

        var item = rows[selectedIndex].Item;
        var (quantity, _) = PaletteQuery.Parse(search?.Text, options.AllowQuantity);
        var choose = options.Choose;

        if (!keepOpen)
        {
            Close();
            await choose(item, quantity);
            return;
        }

        await choose(item, quantity);
        if (search is not null)
        {
            search.Text = string.Empty;
            search.Focus(NavigationMethod.Tab);
        }
    }

    private void Close()
    {
        if (host is null)
        {
            return;
        }

        host.Children.Remove(this);
        host = null;
        var released = backdrop;
        SetBackdrop(null);
        released?.Dispose();
        if (ReferenceEquals(current, this))
        {
            current = null;
        }

        if (returnFocusTo is Visual visual && visual.IsAttachedToVisualTree())
        {
            returnFocusTo.Focus();
        }

        closed?.TrySetResult();
    }
}
