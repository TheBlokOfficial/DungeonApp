using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace DungeonApp.Views.Components;

public partial class RegistryLayoutControl : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<RegistryLayoutControl, string>(nameof(Title), string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<string> TotalCountTextProperty =
        AvaloniaProperty.Register<RegistryLayoutControl, string>(nameof(TotalCountText), "0 elementów");

    public string TotalCountText
    {
        get => GetValue(TotalCountTextProperty);
        set => SetValue(TotalCountTextProperty, value);
    }

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<RegistryLayoutControl, IEnumerable?>(nameof(ItemsSource));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<RegistryLayoutControl, IDataTemplate?>(nameof(ItemTemplate));

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly StyledProperty<object?> ListHeaderProperty =
        AvaloniaProperty.Register<RegistryLayoutControl, object?>(nameof(ListHeader));

    public object? ListHeader
    {
        get => GetValue(ListHeaderProperty);
        set => SetValue(ListHeaderProperty, value);
    }

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<RegistryLayoutControl, object?>(nameof(SelectedItem));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> DetailTemplateProperty =
        AvaloniaProperty.Register<RegistryLayoutControl, IDataTemplate?>(nameof(DetailTemplate));

    public IDataTemplate? DetailTemplate
    {
        get => GetValue(DetailTemplateProperty);
        set => SetValue(DetailTemplateProperty, value);
    }

    public RegistryLayoutControl()
    {
        InitializeComponent();
    }
}
