using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
public sealed class EntityCardViewModel : ObservableObject, IPanelBody, IDisposable
{
    /// <summary>What the preview says while it has nothing to show.</summary>
    public const string EmptyText = "Kliknij pozycję w katalogu Świat";

    private readonly WorldCatalogViewModel _catalog;
    private readonly EntityCardBuilder _builder;
    private readonly bool _followsSelection;
    private readonly RelayCommand? _pin;
    private (CampaignEntity Entity, string Label)? _fingerprint;
    private bool _isDisposed;

    public EntityCardViewModel(
        WorldCatalogViewModel catalog, EntityCardBuilder builder, EntityId? entity, bool followsSelection, Action<EntityId>? pin)
    {
        _catalog = catalog;
        _builder = builder;
        _followsSelection = followsSelection;
        Entity = followsSelection ? catalog.LastClickedEntity : entity;

        if (pin is not null)
        {
            _pin = new RelayCommand(() =>
            {
                if (Entity is { } shown)
                {
                    pin(shown);
                }
            }, () => Entity is not null);
            HeaderActions = [new PanelHeaderAction("DungeonIconPin", "Przypnij kartę w osobnym oknie", _pin)];
        }

        Rebuild();
        catalog.TreeChanged += OnTreeChanged;

        if (followsSelection)
        {
            catalog.PropertyChanged += OnCatalogPropertyChanged;
        }
    }

    /// <summary>The entity shown; null - the preview is empty.</summary>
    public EntityId? Entity { get; private set; }

    /// <summary>The card, or the reason there is none; null while empty.</summary>
    public ContentDetailViewModel? Detail { get; private set; }

    public bool IsEmpty => Detail is null;

    public IReadOnlyList<PanelHeaderAction> HeaderActions { get; } = [];

    /// <summary>"Podgląd", or an entity's name and № for its own window.</summary>
    public string? Title
    {
        get
        {
            if (_followsSelection)
            {
                return "Podgląd";
            }

            return Entity is { } id && _catalog.Tree.Entity(id) is { } node
                ? $"{node.Name} · № {node.Entity.Number.ToString(CultureInfo.InvariantCulture)}"
                : null;
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
        OnPropertyChanged(nameof(Title));
        _pin?.NotifyCanExecuteChanged();
    }
}
