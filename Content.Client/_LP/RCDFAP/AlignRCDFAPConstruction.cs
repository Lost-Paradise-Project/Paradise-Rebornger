using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared._LP.RCDFAP.Components;
using Content.Shared._LP.RCDFAP.Systems;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Client.Hands.Systems;
using Content.Shared.RCD.Components;
using Content.Shared.RCD.Systems;

namespace Content.Client._LP.RCDFAP;

public sealed class AlignRCDFAPConstruction : PlacementMode
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly SharedMapSystem _mapSystem;
    private readonly HandsSystem _handsSystem;
    private readonly RCDFAPSystem _rcdfapSystem;
    private readonly SharedTransformSystem _transformSystem;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private const float SearchBoxSize = 2f;
    private const float PlaceColorBaseAlpha = 0.5f;

    private EntityCoordinates _unalignedMouseCoords = default;

    /// <summary>
    /// This placement mode is not on the engine because it is content specific (i.e., for the RCDFAP)
    /// </summary>
    public AlignRCDFAPConstruction(PlacementManager pMan) : base(pMan)
    {
        IoCManager.InjectDependencies(this);
        _mapSystem = _entityManager.System<SharedMapSystem>();
        _handsSystem = _entityManager.System<HandsSystem>();
        _rcdfapSystem = _entityManager.System<RCDFAPSystem>();
        _transformSystem = _entityManager.System<SharedTransformSystem>();

        ValidPlaceColor = ValidPlaceColor.WithAlpha(PlaceColorBaseAlpha);
    }

    public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
    {
        _unalignedMouseCoords = ScreenToCursorGrid(mouseScreen);
        MouseCoords = _unalignedMouseCoords.AlignWithClosestGridTile(SearchBoxSize, _entityManager, _mapManager);

        var gridId = _transformSystem.GetGrid(MouseCoords);

        if (!_entityManager.TryGetComponent<MapGridComponent>(gridId, out var mapGrid))
            return;

        CurrentTile = _mapSystem.GetTileRef(gridId.Value, mapGrid, MouseCoords);

        float tileSize = mapGrid.TileSize;
        GridDistancing = tileSize;

        if (pManager.CurrentPermission!.IsTile)
        {
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2,
                CurrentTile.Y + tileSize / 2));
        }
        else
        {
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));
        }
    }

    public override bool IsValidPosition(EntityCoordinates position)
    {
        var player = _playerManager.LocalSession?.AttachedEntity;

        // If the destination is out of interaction range, set the placer alpha to zero
        if (!_entityManager.TryGetComponent<TransformComponent>(player, out var xform))
            return false;

        if (!_transformSystem.InRange(xform.Coordinates, position, SharedInteractionSystem.InteractionRange))
        {
            InvalidPlaceColor = InvalidPlaceColor.WithAlpha(0);
            return false;
        }

        // Otherwise restore the alpha value
        else
        {
            InvalidPlaceColor = InvalidPlaceColor.WithAlpha(PlaceColorBaseAlpha);
        }

        // Determine if player is carrying an RCDFAP in their active hand
        if (!_handsSystem.TryGetActiveItem(player.Value, out var heldEntity))
            return false;

        if (!_entityManager.TryGetComponent<RCDFAPComponent>(heldEntity, out var rcdfap))
            return false;

        var gridUid = _transformSystem.GetGrid(position);
        if (!_entityManager.TryGetComponent<MapGridComponent>(gridUid, out var mapGrid))
            return false;
        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, position);
        var posVector = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, position);

        // Determine if the user is hovering over a target
        var currentState = _stateManager.CurrentState;

        if (currentState is not GameplayStateBase screen)
            return false;

        var target = screen.GetClickedEntity(_transformSystem.ToMapCoordinates(_unalignedMouseCoords));

        // Determine if the RCDFAP operation is valid or not
        if (!_rcdfapSystem.IsRCDFAPOperationStillValid(heldEntity.Value, rcdfap, gridUid.Value, mapGrid, tile, posVector, target, player.Value, false))
            return false;

        return true;
    }
}
