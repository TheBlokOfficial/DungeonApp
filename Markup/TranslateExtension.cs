using Avalonia.Data;
using Avalonia.Markup.Xaml;
using DungeonApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DungeonApp.Markup;

public class TranslateExtension : MarkupExtension
{
    public string Key { get; }

    public TranslateExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var translationService = App.Current?.Services?.GetService<ITranslationService>();
        if (translationService == null) return Key;

        // Bindowanie do indexera w serwisie: this[string key]
        // Dzięki temu, że TranslationService powiadamia o zmianie "Item[]",
        // Avalonia automatycznie zaktualizuje wszystkie użycia {i18n:Translate ...}
        var binding = new Binding($"[{Key}]")
        {
            Mode = BindingMode.OneWay,
            Source = translationService
        };

        return binding;
    }
}
