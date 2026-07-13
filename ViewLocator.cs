using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DungeonApp.ViewModels;

namespace DungeonApp;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    private readonly ConditionalWeakTable<object, Control> _viewCache = new();

    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        // BARDZO WAŻNE: Avalonia rozmontowuje widok z drzewa wizualnego, gdy TransitioningContentControl kończy animację.
        // Zbuforowanie widoku w ConditionalWeakTable sprawia, że przy kolejnym wejściu w tę samą zakładkę (Singleton ViewModel),
        // nie ma żadnego (0ms) opóźnienia na kompilację JIT czy parsowanie XAML. To całkowicie eliminuje "stuttery"!
        if (_viewCache.TryGetValue(param, out var cachedView))
        {
            return cachedView;
        }

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            var view = (Control)Activator.CreateInstance(type)!;
            _viewCache.Add(param, view);
            return view;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
