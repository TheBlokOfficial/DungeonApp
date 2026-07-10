using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Services;

namespace DungeonApp.ViewModels.Dashboard;

public abstract partial class RegistryTabViewModelBase : ViewModelBase
{
    protected readonly IContentRegistry _contentRegistry;
    protected readonly ITranslationService _translationService;

    public ObservableCollection<FilterItemViewModel> PackFilters { get; } = new();
    public ObservableCollection<FilterItemViewModel> TypeFilters { get; } = new();
    public ObservableCollection<FilterItemViewModel> RarityFilters { get; } = new();
    public ObservableCollection<FilterItemViewModel> SizeFilters { get; } = new();
    public ObservableCollection<FilterItemViewModel> ChallengeRatingFilters { get; } = new();
    public ObservableCollection<FilterItemViewModel> TagFilters { get; } = new();
    public ObservableCollection<FilterItemViewModel> FactionFilters { get; } = new();
    public ObservableCollection<NumericFilterViewModel> NumericFilters { get; } = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    protected RegistryTabViewModelBase(IContentRegistry contentRegistry, ITranslationService translationService)
    {
        _contentRegistry = contentRegistry;
        _translationService = translationService;

        _translationService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ITranslationService.CurrentLanguage))
            {
                LoadData();
            }
        };
    }

    protected abstract void LoadData();

    protected void OnFilterChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnFiltersChanged();
    }

    protected abstract void OnFiltersChanged();

    [RelayCommand]
    private void Refresh() => LoadData();

    protected void ClearFilterSubscriptions()
    {
        foreach (var pf in PackFilters) pf.PropertyChanged -= OnFilterChanged;
        foreach (var tf in TypeFilters) tf.PropertyChanged -= OnFilterChanged;
        foreach (var rf in RarityFilters) rf.PropertyChanged -= OnFilterChanged;
        foreach (var sf in SizeFilters) sf.PropertyChanged -= OnFilterChanged;
        foreach (var cf in ChallengeRatingFilters) cf.PropertyChanged -= OnFilterChanged;
        foreach (var tgf in TagFilters) tgf.PropertyChanged -= OnFilterChanged;
        foreach (var ff in FactionFilters) ff.PropertyChanged -= OnFilterChanged;
        foreach (var nf in NumericFilters) nf.PropertyChanged -= OnFilterChanged;
    }
}
