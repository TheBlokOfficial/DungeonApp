using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Controls.Content;

/// <summary>
/// A reusable card control: an optional title over a block of static text. See
/// <see cref="TraitListView"/>'s remarks - the same reasoning about direct properties, XAML
/// composition and self-hosted <see cref="Avalonia.StyledElement.DataContext"/> applies here.
/// </summary>
public partial class ProseBlockView : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ProseBlockView, string?>(nameof(Title));

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ProseBlockView, string?>(nameof(Text));

    public static readonly DirectProperty<ProseBlockView, bool> HasTitleProperty =
        AvaloniaProperty.RegisterDirect<ProseBlockView, bool>(nameof(HasTitle), o => o.HasTitle);

    private bool _hasTitle;

    public ProseBlockView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool HasTitle
    {
        get => _hasTitle;
        private set => SetAndRaise(HasTitleProperty, ref _hasTitle, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty)
        {
            HasTitle = !string.IsNullOrEmpty(Title);
        }
    }
}
