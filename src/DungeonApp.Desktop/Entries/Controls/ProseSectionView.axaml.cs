using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace DungeonApp.Desktop.Entries.Controls;

/// <summary>
/// A reusable card control: one titled section of a card's text - a heading, an optional introduction and a list of <see cref="ProseItem"/>s. Static: it neither collapses nor
/// reacts to the pointer. A card view composes it in XAML and sets its properties directly; like
/// <see cref="TraitListView"/>, it is its own <see cref="StyledElement.DataContext"/>.
/// </summary>
public partial class ProseSectionView : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ProseSectionView, string?>(nameof(Title));

    public static readonly StyledProperty<string?> IntroProperty =
        AvaloniaProperty.Register<ProseSectionView, string?>(nameof(Intro));

    public static readonly StyledProperty<IReadOnlyList<ProseItem>> ItemsProperty =
        AvaloniaProperty.Register<ProseSectionView, IReadOnlyList<ProseItem>>(nameof(Items), defaultValue: []);

    public ProseSectionView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The paragraph before the items; may carry <c>**…**</c> emphasis.</summary>
    public string? Intro
    {
        get => GetValue(IntroProperty);
        set => SetValue(IntroProperty, value);
    }

    public IReadOnlyList<ProseItem> Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
}
