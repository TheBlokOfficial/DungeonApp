using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;

namespace DungeonApp.UI;

public class FluentEntranceTransition : IPageTransition
{
    private readonly TimeSpan _duration;
    private readonly bool _forward;

    public FluentEntranceTransition(TimeSpan duration, bool forward)
    {
        _duration = duration;
        _forward = forward;
    }

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var tasks = new List<Task>();

        if (from is Control fromControl)
        {
            var fadeOut = new Animation
            {
                Duration = _duration,
                FillMode = FillMode.Forward,
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, 1d) } },
                    new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, 0d) } }
                }
            };
            tasks.Add(fadeOut.RunAsync(fromControl, cancellationToken));
        }

        if (to is Control toControl)
        {
            toControl.Opacity = 0;
            // 40 pixels offset gives a nice directional feel without moving the whole screen
            var offset = _forward ? 40.0 : -40.0;
            
            var entrance = new Animation
            {
                Duration = _duration,
                FillMode = FillMode.Forward,
                Easing = new CubicEaseOut(),
                Children =
                {
                    new KeyFrame { 
                        Cue = new Cue(0d), 
                        Setters = { 
                            new Setter(Visual.OpacityProperty, 0d),
                            new Setter(Layoutable.MarginProperty, new Thickness(offset, 0, -offset, 0))
                        } 
                    },
                    new KeyFrame { 
                        Cue = new Cue(1d), 
                        Setters = { 
                            new Setter(Visual.OpacityProperty, 1d),
                            new Setter(Layoutable.MarginProperty, new Thickness(0))
                        } 
                    }
                }
            };

            tasks.Add(entrance.RunAsync(toControl, cancellationToken));
        }

        await Task.WhenAll(tasks);
        
        if (from is Control oldControl)
        {
            oldControl.Opacity = 1d;
        }
    }
}
