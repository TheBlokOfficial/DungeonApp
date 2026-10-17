using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DungeonApp.Core.Entries;
using DungeonApp.Core.Entries.Entities;
using DungeonApp.Core.World;
using DungeonApp.Desktop.Entries.ContentTab;
using DungeonApp.Desktop.Workspace.Panels;

namespace DungeonApp.Desktop.Workspace.World;

/// <summary>
/// The body of a window that shows an entity's card: the preview (follows the catalog's last clicked
/// entity, has a pin) or a pinned window (one entity for good). Read-only. Rebuilds the card when
/// the entity's name, path or values change and empties when the entity is gone.
/// </summary>
public sealed class EntityCardViewModel : ObservableObject, IPanelBody, IPanelTitleTarget, IDisposable
{
    /// <summary>What the preview says while it has nothing to show.</summary>
    public const string EmptyText = "Kliknij pozycję w katalogu Świat";

    private readonly WorldCatalogViewModel _catalog;
    private readonly EntityCardBuilder _builder;
    private bool _followsSelection;
    private readonly RelayCommand? _pin;
    private readonly PanelHeaderAction[] _pinAction = [];
    private readonly PanelHeaderAction[] _unpinAction = [];
    private (CampaignEntity Entity, string Label)? _fingerprint;
    private bool _isDisposed;

    public EntityCardViewModel(
        WorldCatalogViewModel catalog, EntityCardBuilder builder, EntityId? entity, bool followsSelection, Action<EntityCardViewModel>? togglePin)
    {
        _catalog = catalog;
        _builder = builder;
        _followsSelection = followsSelection;
        Entity = followsSelection ? catalog.LastClickedEntity : entity;

        // The one button flips the window's role: pin on a preview, unpin on a pinned card.
        if (togglePin is not null)
        {
            _pin = new RelayCommand(() => togglePin(this), () => !_followsSelection || Entity is not null);
            _pinAction = [new PanelHeaderAction("DungeonIconPin", "Przypnij kartę", _pin)];
            _unpinAction = [new PanelHeaderAction("DungeonIconPinned", "Odepnij kartę", _pin)];
        }

        Rebuild();
        catalog.TreeChanged += OnTreeChanged;

        if (followsSelection)
        {
            catalog.PropertyChanged += OnCatalogPropertyChanged;
        }
    }

    /// <summary>True while this is the preview (follows the catalog), false as a pinned card.</summary>
    public bool FollowsSelection => _followsSelection;

    /// <summary>The preview turns into the pinned card of the entity it shows; the window stays.</summary>
    public void BecomePinned()
    {
        _followsSelection = false;
        _catalog.PropertyChanged -= OnCatalogPropertyChanged;
        NotifyRoleChanged();
    }

    /// <summary>The pinned card turns into the preview and shows what the catalog last clicked.</summary>
    public void BecomePreview()
    {
        _followsSelection = true;
        _catalog.PropertyChanged -= OnCatalogPropertyChanged;
        _catalog.PropertyChanged += OnCatalogPropertyChanged;
        Entity = _catalog.LastClickedEntity;
        Rebuild();
        NotifyRoleChanged();
    }

    private void NotifyRoleChanged()
    {
        OnPropertyChanged(nameof(HeaderActions));
        _pin?.NotifyCanExecuteChanged();
    }

    /// <summary>The entity shown; null - the preview is empty.</summary>
    public EntityId? Entity { get; private set; }

    /// <summary>The card, or the reason there is none; null while empty.</summary>
    public ContentDetailViewModel? Detail { get; private set; }

    public bool IsEmpty => Detail is null;

    public IReadOnlyList<PanelHeaderAction> HeaderActions => _followsSelection ? _pinAction : _unpinAction;

    /// <summary>
    /// Says what the window is and never changes; that it is pinned the pin in the header says, and
    /// the entity's name stands in the card.
    /// </summary>
    public string? Title => "Podgląd";

    /// <summary>A click on the window's title shows the entity in the catalog's tree.</summary>
    public void TitleClicked()
    {
        if (Entity is { } id)
        {
            _catalog.Reveal(id);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _catalog.TreeChanged -= OnTreeChanged;
        _catalog.PropertyChanged -= OnCatalogPropertyChanged;
    }

    private void OnCatalogPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorldCatalogViewModel.LastClickedEntity) && Entity != _catalog.LastClickedEntity)
        {
            Entity = _catalog.LastClickedEntity;
            Rebuild();
        }
    }

    private void OnTreeChanged()
    {
        if (_followsSelection && Entity != _catalog.LastClickedEntity)
        {
            Entity = _catalog.LastClickedEntity;
            Rebuild();
            return;
        }

        if (Fingerprint() != _fingerprint)
        {
            Rebuild();
        }
    }

    // What the card is made of: the entity (its patch included), its name and its path. A change in
    // another entity of the tree leaves the card as it is.
    private (CampaignEntity Entity, string Label)? Fingerprint() =>
        Entity is { } id && _catalog.Tree.Entity(id) is { } node
            ? (node.Entity, string.Join('/', [node.Name, .. _catalog.Tree.EntityPath(id)]))
            : null;

    private void Rebuild()
    {
        _fingerprint = Fingerprint();
        Detail = Entity is { } id ? _builder.Build(_catalog.Tree, id) : null;

        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(IsEmpty));
        _pin?.NotifyCanExecuteChanged();
    }
}
