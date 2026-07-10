using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DungeonApp.Models.ContentPacks;

namespace DungeonApp.ViewModels.Dashboard;

public partial class DamageFilterViewModel : ViewModelBase
{
    private readonly Services.ITranslationService _translationService;

    [ObservableProperty] private bool _isActive;

    [ObservableProperty] private double _absoluteMin;
    [ObservableProperty] private double _absoluteMax;
    [ObservableProperty] private double _currentMin;
    [ObservableProperty] private double _currentMax;

    public ObservableCollection<FilterItemViewModel> DiceSides { get; } = new();
    public ObservableCollection<FilterItemViewModel> DamageTypes { get; } = new();

    public DamageFilterViewModel(Services.ITranslationService translationService)
    {
        _translationService = translationService;
    }

    /// <summary>
    /// Pobiera wszystkie obiekty obrażeń z całej dostępnej puli (bez filtrów)
    /// i na ich podstawie buduje zakres suwaka i listę dostępnych opcji.
    /// </summary>
    public void Populate(IEnumerable<DamageDefinition> allDamages)
    {
        var validDamages = allDamages.Where(d => d.IsValid).ToList();
        
        if (validDamages.Count == 0)
        {
            IsActive = false;
            return;
        }

        IsActive = true;

        // 1. Suwak średnich obrażeń
        AbsoluteMin = validDamages.Min(d => d.AverageDamage);
        AbsoluteMax = validDamages.Max(d => d.AverageDamage);
        
        // Zabezpieczenie przed Min == Max
        if (Math.Abs(AbsoluteMin - AbsoluteMax) < 0.1)
        {
            AbsoluteMin = 0;
        }
        
        CurrentMin = AbsoluteMin;
        CurrentMax = AbsoluteMax;

        // 2. Kości (d4, d6, d8...)
        DiceSides.Clear();
        var distinctSides = validDamages.Select(d => d.DiceSides).Distinct().OrderBy(d => d).ToList();
        foreach (var side in distinctSides)
        {
            var item = new FilterItemViewModel($"d{side}", side.ToString());
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(FilterItemViewModel.IsSelected)) OnFilterChanged(); };
            DiceSides.Add(item);
        }

        // 3. Typy obrażeń
        DamageTypes.Clear();
        var distinctTypes = validDamages.Select(d => d.Type).Distinct().OrderBy(t => t).ToList();
        foreach (var t in distinctTypes)
        {
            if (string.IsNullOrEmpty(t)) continue;
            var item = new FilterItemViewModel(_translationService.Translate(t), t);
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(FilterItemViewModel.IsSelected)) OnFilterChanged(); };
            DamageTypes.Add(item);
        }
    }

    public event Action? FilterChanged;

    private void OnFilterChanged()
    {
        FilterChanged?.Invoke();
    }

    partial void OnCurrentMinChanged(double value) => OnFilterChanged();
    partial void OnCurrentMaxChanged(double value) => OnFilterChanged();

    /// <summary>
    /// Zwraca TRUE, jeśli podana lista obrażeń z przedmiotu/potwora spełnia warunki tego filtra.
    /// </summary>
    public bool PassesFilter(List<DamageDefinition> damages)
    {
        if (!IsActive || damages == null || damages.Count == 0)
            return true; // Brak obrażeń u przedmiotu a filtr jest aktywny? Zakładamy przepuszczanie lub odrzucanie?
                         // Skoro filtrujemy "broń", jeśli szukamy d8, to przedmiot bez d8 odpada.
                         // Oceniamy tylko elementy, które MAJĄ obrażenia.

        // Jeśli jakakolwiek definicja obrażeń spełnia wszystkie filtry, przedmiot jest przepuszczany.
        
        // Wybrane kości
        var selectedSides = DiceSides.Where(d => d.IsSelected).Select(d => int.Parse(d.Value)).ToList();
        // Wybrane typy
        var selectedTypes = DamageTypes.Where(t => t.IsSelected).Select(t => t.Value).ToList();

        // Jeśli nic nie wybrano w sekcji kości/typów, zachowują się jakby przepuszczały wszystko.
        
        foreach (var dmg in damages)
        {
            if (!dmg.IsValid) continue;

            // Sprawdź suwak
            if (dmg.AverageDamage < CurrentMin || dmg.AverageDamage > CurrentMax)
                continue;

            // Sprawdź kości
            if (selectedSides.Count > 0 && !selectedSides.Contains(dmg.DiceSides))
                continue;

            // Sprawdź typ
            if (selectedTypes.Count > 0 && !selectedTypes.Contains(dmg.Type))
                continue;

            // Jeśli przeszedł wszystkie 3 checki (na tej jednej konkretnej definicji "1d8 slashing"), to przedmiot pasuje.
            return true;
        }

        // Jeśli żadna z definicji nie pasowała do połączonych warunków - odrzucamy.
        return false;
    }
}
