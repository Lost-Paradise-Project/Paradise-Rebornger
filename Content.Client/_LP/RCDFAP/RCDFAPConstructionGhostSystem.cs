using Content.Client.Hands.Systems;
using Content.Shared.Interaction;
using Content.Shared._LP.RCDFAP;
using Content.Shared._LP.RCDFAP.Components;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._LP.RCDFAP;

/// <summary>
/// System for handling structure ghost placement in places where RCDFAP can create objects.
/// </summary>
public sealed class RCDFAPConstructionGhostSystem : EntitySystem
{
    private const string PlacementMode = nameof(AlignRCDFAPConstruction);

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly HandsSystem _hands = default!;

    private Direction _placementDirection = default;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Get current placer data
        var placerEntity = _placementManager.CurrentPermission?.MobUid;
        var placerProto = _placementManager.CurrentPermission?.EntityType;
        var placerIsRCDFAP = HasComp<RCDFAPComponent>(placerEntity);

        // Exit if erasing or the current placer is not an RCDFAP (build mode is active)
        if (_placementManager.Eraser || (placerEntity != null && !placerIsRCDFAP))
            return;

        // Determine if player is carrying an RCDFAP in their active hand
        if (_playerManager.LocalSession?.AttachedEntity is not { } player)
            return;

        var heldEntity = _hands.GetActiveItem(player);

        // Don't open the placement overlay for client-side RCDFAPs.
        // This may happen when predictively spawning one in your hands.
        if (heldEntity != null && IsClientSide(heldEntity.Value))
            return;

        if (!TryComp<RCDFAPComponent>(heldEntity, out var rcdfap))
        {
            // If the player was holding an RCDFAP, but is no longer, cancel placement
            if (placerIsRCDFAP)
                _placementManager.Clear();

            return;
        }
        var prototype = _protoManager.Index(rcdfap.ProtoId);

        // Update the direction the RCDFAP prototype based on the placer direction
        if (_placementDirection != _placementManager.Direction)
        {
            _placementDirection = _placementManager.Direction;
            RaiseNetworkEvent(new RCDFAPConstructionGhostRotationEvent(GetNetEntity(heldEntity.Value), _placementDirection));
        }

        // If the placer has not changed, exit
        if (heldEntity == placerEntity && prototype.Prototype == placerProto)
            return;

        // Create a new placer
        var newObjInfo = new PlacementInformation
        {
            MobUid = heldEntity.Value,
            PlacementOption = PlacementMode,
            EntityType = prototype.Prototype,
            Range = (int)Math.Ceiling(SharedInteractionSystem.InteractionRange),
            IsTile = (prototype.Mode == RcdfapMode.ConstructTile),
            UseEditorContext = false,
        };

        _placementManager.Clear();
        _placementManager.BeginPlacing(newObjInfo);
    }
}
