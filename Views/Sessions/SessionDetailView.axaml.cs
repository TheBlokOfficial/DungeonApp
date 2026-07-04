using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace DungeonApp.Views;

public partial class SessionDetailView : UserControl
{
    public SessionDetailView()
    {
        InitializeComponent();
        
        // Zgodnie z decyzją użytkownika: Notatki są domyślnie włączone.
        NavNotesToggle.IsChecked = true;
        NavCombatToggle.IsChecked = false;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        if (this.VisualRoot is Window window && window.DataContext is DungeonApp.ViewModels.MainWindowViewModel mainVm)
        {
            mainVm.PropertyChanged -= MainVm_PropertyChanged;
            mainVm.PropertyChanged += MainVm_PropertyChanged;
        }
        
        UpdateLayoutFromToggles();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        if (this.VisualRoot is Window window && window.DataContext is DungeonApp.ViewModels.MainWindowViewModel mainVm)
        {
            mainVm.PropertyChanged -= MainVm_PropertyChanged;
        }
    }

    private void MainVm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DungeonApp.ViewModels.MainWindowViewModel.EffectiveUiScale))
        {
            UpdateLayoutFromToggles();
        }
    }

    private void NavToggle_Click(object? sender, RoutedEventArgs e)
    {
        // Zabezpieczenie przed odznaczeniem obu zakładek na raz
        if (NavNotesToggle.IsChecked == false && NavCombatToggle.IsChecked == false)
        {
            // Przywracamy zaznaczenie tego przycisku, który został właśnie kliknięty (odznaczony)
            if (sender is ToggleButton clickedToggle)
            {
                clickedToggle.IsChecked = true;
            }
        }
        
        UpdateLayoutFromToggles();
    }

    private void UpdateLayoutFromToggles()
    {
        bool showNotes = NavNotesToggle.IsChecked == true;
        bool showCombat = NavCombatToggle.IsChecked == true;
        
        double scale = 1.0;
        if (this.VisualRoot is Window window && window.DataContext is DungeonApp.ViewModels.MainWindowViewModel mainVm)
        {
            scale = mainVm.EffectiveUiScale;
        }
        
        double activeMinWidth = 600 / scale;
        
        if (showNotes && showCombat)
        {
            // Widok podzielony
            NotesPanel.IsVisible = true;
            CombatPanel.IsVisible = true;
            PanelSplitter.IsVisible = true;
            
            // Przywracamy domyślny podział 50/50 oraz minimalną szerokość
            ContentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            ContentGrid.ColumnDefinitions[0].MinWidth = activeMinWidth;
            
            ContentGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            ContentGrid.ColumnDefinitions[2].MinWidth = activeMinWidth;
            
            Grid.SetColumn(NotesPanel, 0);
            Grid.SetColumnSpan(NotesPanel, 1);
            
            Grid.SetColumn(CombatPanel, 2);
            Grid.SetColumnSpan(CombatPanel, 1);
        }
        else if (showNotes && !showCombat)
        {
            // Tylko Notatki
            NotesPanel.IsVisible = true;
            CombatPanel.IsVisible = false;
            PanelSplitter.IsVisible = false;
            
            ContentGrid.ColumnDefinitions[0].MinWidth = 0;
            ContentGrid.ColumnDefinitions[2].MinWidth = 0;
            
            // Notatki na pełną szerokość
            Grid.SetColumn(NotesPanel, 0);
            Grid.SetColumnSpan(NotesPanel, 3);
        }
        else if (!showNotes && showCombat)
        {
            // Tylko Walka
            NotesPanel.IsVisible = false;
            CombatPanel.IsVisible = true;
            PanelSplitter.IsVisible = false;
            
            ContentGrid.ColumnDefinitions[0].MinWidth = 0;
            ContentGrid.ColumnDefinitions[2].MinWidth = 0;
            
            // Walka na pełną szerokość (wymuszone przemieszczenie do Col 0 ze Span 3)
            Grid.SetColumn(CombatPanel, 0);
            Grid.SetColumnSpan(CombatPanel, 3);
        }
    }
}