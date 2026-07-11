using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace DungeonApp.Views.Controls;

public partial class BadgeControl : UserControl
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<BadgeControl, string>(nameof(Text));

    public static readonly StyledProperty<IBrush?> BadgeBackgroundProperty =
        AvaloniaProperty.Register<BadgeControl, IBrush?>(nameof(BadgeBackground));

    public static readonly StyledProperty<Thickness> BadgeBorderThicknessProperty =
        AvaloniaProperty.Register<BadgeControl, Thickness>(nameof(BadgeBorderThickness), new Thickness(0));

    public static readonly StyledProperty<IBrush?> BadgeBorderBrushProperty =
        AvaloniaProperty.Register<BadgeControl, IBrush?>(nameof(BadgeBorderBrush));

    public static readonly StyledProperty<IEffect?> GlowEffectProperty =
        AvaloniaProperty.Register<BadgeControl, IEffect?>(nameof(GlowEffect));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IBrush? BadgeBackground
    {
        get => GetValue(BadgeBackgroundProperty);
        set => SetValue(BadgeBackgroundProperty, value);
    }

    public Thickness BadgeBorderThickness
    {
        get => GetValue(BadgeBorderThicknessProperty);
        set => SetValue(BadgeBorderThicknessProperty, value);
    }

    public IBrush? BadgeBorderBrush
    {
        get => GetValue(BadgeBorderBrushProperty);
        set => SetValue(BadgeBorderBrushProperty, value);
    }

    public static readonly StyledProperty<double> BadgeFontSizeProperty =
        AvaloniaProperty.Register<BadgeControl, double>(nameof(BadgeFontSize), 11.0);

    public static readonly StyledProperty<Thickness> BadgePaddingProperty =
        AvaloniaProperty.Register<BadgeControl, Thickness>(nameof(BadgePadding), new Thickness(6, 2));

    public static readonly StyledProperty<CornerRadius> BadgeCornerRadiusProperty =
        AvaloniaProperty.Register<BadgeControl, CornerRadius>(nameof(BadgeCornerRadius), new CornerRadius(4));

    public double BadgeFontSize
    {
        get => GetValue(BadgeFontSizeProperty);
        set => SetValue(BadgeFontSizeProperty, value);
    }

    public Thickness BadgePadding
    {
        get => GetValue(BadgePaddingProperty);
        set => SetValue(BadgePaddingProperty, value);
    }

    public CornerRadius BadgeCornerRadius
    {
        get => GetValue(BadgeCornerRadiusProperty);
        set => SetValue(BadgeCornerRadiusProperty, value);
    }

    public IEffect? GlowEffect
    {
        get => GetValue(GlowEffectProperty);
        set => SetValue(GlowEffectProperty, value);
    }

    public BadgeControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
