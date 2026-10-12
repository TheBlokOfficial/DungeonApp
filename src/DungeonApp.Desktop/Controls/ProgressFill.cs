using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DungeonApp.Desktop.Controls;

/// <summary>
/// Determinate progress fill (part of the ProgressBar template, Themes/Controls/ProgressBar.axaml):
/// draws <see cref="DisplayedPercentage"/> of its width, and that displayed value chases
/// <see cref="Percentage"/> instead of being animated by a transition. Every frame
/// (<see cref="TopLevel.RequestAnimationFrame"/>) covers a share of the remaining distance that
/// depends only on the frame's time: half of it per DungeonProgressFillHalfLife, never slower than
/// <see cref="MinSpeed"/>. A new value only
/// moves the target, so a run of quick changes never restarts the motion from a standstill (an
/// eased transition restarted on every change barely leaves its slow start) and the fill never
/// jumps back; each new target reads as an ease-out from wherever the fill is.
/// <para>
/// A frame long after the previous one (the UI thread held by work) counts as at most
/// <see cref="MaxFrameStep"/>: the fill keeps sliding in visible steps instead of jumping to the
/// target in one frame. Without motion (system animations disabled, the control detached or hidden)
/// the fill stands on its target at once; the first value the control shows is never animated.
/// Changes appearance only, never application state.
/// </para>
/// </summary>
public sealed class ProgressFill : Control
{
    /// <summary>
    /// How many half-lives a glide takes at most (<see cref="SettleLimit"/>), with room to spare: a
    /// glide over the whole bar halves its way about five times before <see cref="MinSpeed"/> takes
    /// over, and that finishes within two more.
    /// </summary>
    public const int SettleHalfLives = 10;

    /// <summary>Distance, in pixels, from which the fill stands on its target.</summary>
    private const double SnapDistance = 0.5;

    /// <summary>
    /// Slowest glide, in percent of the bar per second. The halving alone leaves a long tail of
    /// barely moving frames; at this speed the last few percent end in a short, still gentle stop.
    /// </summary>
    private const double MinSpeed = 25;

    private static readonly TimeSpan MaxFrameStep = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan FallbackHalfLife = TimeSpan.FromMilliseconds(80);

    public static readonly StyledProperty<double> PercentageProperty =
        AvaloniaProperty.Register<ProgressFill, double>(nameof(Percentage));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ProgressFill, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ProgressFill, CornerRadius>(nameof(CornerRadius));

    private readonly Stopwatch frameClock = new();
    private bool chasing;
    private bool drawn;
    private bool frameRequested;
    private TaskCompletionSource? settled;

    static ProgressFill()
    {
        AffectsRender<ProgressFill>(FillProperty, CornerRadiusProperty);
    }

    /// <summary>Target fill, 0-100 (ProgressBar.Percentage).</summary>
    public double Percentage
    {
        get => GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>The fill drawn now, 0-100; reaches <see cref="Percentage"/> frame by frame.</summary>
    public double DisplayedPercentage { get; private set; }

    /// <summary>
    /// The longest a glide takes with frames coming. A waiter on <see cref="WhenSettledAsync"/> bounds
    /// its wait by this: frames may stop coming (a minimized window), and then the fill never arrives.
    /// </summary>
    public TimeSpan SettleLimit => HalfLife() * SettleHalfLives;

    // A value the fill has never drawn is shown as it is: a bar appearing with its value already set
    // (ProgressBar hands it over only during its first layout) does not glide from empty.
    private bool CanMove => drawn && !Themes.SystemMotion.IsReduced && VisualRoot is not null && IsEffectivelyVisible;

    /// <summary>Completes once the drawn fill stands on its target.</summary>
    public Task WhenSettledAsync()
    {
        if (!chasing)
        {
            return Task.CompletedTask;
        }

        settled ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        return settled.Task;
    }

    public override void Render(DrawingContext context)
    {
        drawn = true;
        if (Fill is not { } fill)
        {
            return;
        }

        var width = Bounds.Width * Math.Clamp(DisplayedPercentage, 0, 100) / 100;
        if (width <= 0)
        {
            return;
        }

        context.DrawRectangle(fill, null, new RoundedRect(new Rect(0, 0, width, Bounds.Height), CornerRadius));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        StandOnTarget();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        drawn = false;
        StandOnTarget();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PercentageProperty)
        {
            Chase();
        }
        else if (change.Property == IsVisibleProperty && !IsVisible)
        {
            StandOnTarget();
        }
    }

    private void Chase()
    {
        if (!CanMove)
        {
            StandOnTarget();
            return;
        }

        if (!chasing)
        {
            chasing = true;
            frameClock.Restart();
        }

        RequestFrame();
    }

    private void StandOnTarget()
    {
        chasing = false;
        frameClock.Stop();
        DisplayedPercentage = Percentage;
        InvalidateVisual();
        settled?.TrySetResult();
        settled = null;
    }

    private void RequestFrame()
    {
        if (frameRequested || TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }

        frameRequested = true;
        topLevel.RequestAnimationFrame(OnFrame);
    }

    private void OnFrame(TimeSpan frameTime)
    {
        frameRequested = false;
        if (!chasing)
        {
            return;
        }

        if (!CanMove)
        {
            StandOnTarget();
            return;
        }

        var step = frameClock.Elapsed < MaxFrameStep ? frameClock.Elapsed : MaxFrameStep;
        frameClock.Restart();

        var remaining = Percentage - DisplayedPercentage;
        var covered = 1 - Math.Pow(2, -step.TotalMilliseconds / HalfLife().TotalMilliseconds);
        var distance = Math.Max(Math.Abs(remaining) * covered, MinSpeed * step.TotalSeconds);
        DisplayedPercentage += Math.Sign(remaining) * Math.Min(distance, Math.Abs(remaining));

        if (Math.Abs(Percentage - DisplayedPercentage) * Bounds.Width / 100 < SnapDistance)
        {
            StandOnTarget();
            return;
        }

        InvalidateVisual();
        RequestFrame();
    }

    private TimeSpan HalfLife() =>
        this.TryFindResource("DungeonProgressFillHalfLife", out var value) && value is TimeSpan halfLife && halfLife > TimeSpan.Zero
            ? halfLife
            : FallbackHalfLife;
}
