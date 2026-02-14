using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Systems;
using Content.Shared.Construction;
using Content.Shared._LP.Construction;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared._LP.RCDFAP.Components;
using Content.Shared._LP.RCDFAP;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared._LP.RCDFAP.Systems;

public sealed class RCDFAPSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefMan = default!;
    [Dependency] private readonly FloorTileSystem _floors = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _sharedCharges = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    private readonly int _instantConstructionDelay = 0;
    private readonly ProtoId<RCDFAPPrototype> _deconstructTileProto = "LPPDeconstructTile";
    private readonly ProtoId<RCDFAPPrototype> _deconstructLatticeProto = "LPPDeconstructLattice";
    private static readonly ProtoId<TagPrototype> CatwalkTag = "Catwalk";

    private HashSet<EntityUid> _intersectingEntities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RCDFAPComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RCDFAPComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RCDFAPComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RCDFAPComponent, RCDFAPDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RCDFAPComponent, DoAfterAttemptEvent<RCDFAPDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<RCDFAPComponent, RCDFAPSystemMessage>(OnRCDFAPSystemMessage);
        SubscribeNetworkEvent<RCDFAPConstructionGhostRotationEvent>(OnRCDFAPconstructionGhostRotationEvent);
    }

    #region Event handling

    private void OnMapInit(EntityUid uid, RCDFAPComponent component, MapInitEvent args)
    {
        // On init, set the RCDFAP to its first available recipe
        if (component.AvailablePrototypes.Count > 0)
        {
            component.ProtoId = component.AvailablePrototypes.ElementAt(0);
            Dirty(uid, component);

            return;
        }

        // The RCDFAP has no valid recipes somehow? Get rid of it
        QueueDel(uid);
    }

    private void OnRCDFAPSystemMessage(EntityUid uid, RCDFAPComponent component, RCDFAPSystemMessage args)
    {
        // Exit if the RCDFAP doesn't actually know the supplied prototype
        if (!component.AvailablePrototypes.Contains(args.ProtoId))
            return;

        if (!_protoManager.Resolve<RCDFAPPrototype>(args.ProtoId, out var prototype))
            return;

        // Set the current RCDFAP prototype to the one supplied
        component.ProtoId = args.ProtoId;

        _adminLogger.Add(LogType.RCDFAP, LogImpact.Low, $"{args.Actor} set RCDFAP mode to: {prototype.Mode} : {prototype.Prototype}");

        Dirty(uid, component);
    }

    private void OnExamine(EntityUid uid, RCDFAPComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var prototype = _protoManager.Index(component.ProtoId);

        var msg = Loc.GetString("rcdfap-component-examine-mode-details", ("mode", Loc.GetString(prototype.SetName)));

        if (prototype.Mode == RcdfapMode.ConstructTile || prototype.Mode == RcdfapMode.ConstructObject)
        {
            var name = Loc.GetString(prototype.SetName);

            if (prototype.Prototype != null &&
                _protoManager.TryIndex(prototype.Prototype, out var proto)) // don't use Resolve because this can be a tile
                name = proto.Name;

            msg = Loc.GetString("rcdfap-component-examine-build-details", ("name", name));
        }

        args.PushMarkup(msg);
    }

    private void OnAfterInteract(EntityUid uid, RCDFAPComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        var user = args.User;
        var location = args.ClickLocation;
        var prototype = _protoManager.Index(component.ProtoId);

        // Initial validity checks
        if (!location.IsValid(EntityManager))
            return;

        // Get grid corresponding to user's click location.
        // If that doesn't exist, try using the one they're standing on.
        // In the future we might want to also check adjacent spaces for grids,
        // in case the user is floating in space for whatever reason.
        var clickGridUid = _transform.GetGrid(location);
        var userGridUid = _transform.GetGrid(user);
        var gridUid = clickGridUid.HasValue ? clickGridUid : userGridUid;

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            _popup.PopupClient(Loc.GetString("rcdfap-component-no-valid-grid"), uid, user);
            return;
        }
        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);

        if (!IsRCDFAPOperationStillValid(uid, component, gridUid.Value, mapGrid, tile, position, component.ConstructionDirection, args.Target, args.User))
            return;

        if (!_net.IsServer)
            return;

        // Get the starting cost, delay, and effect from the prototype
        var cost = prototype.Cost;
        var delay = prototype.Delay;
        var effectPrototype = prototype.Effect;

        #region: Operation modifiers

        // Deconstruction modifiers
        switch (prototype.Mode)
        {
            case RcdfapMode.Deconstruct:

                // Deconstructing an object
                if (args.Target != null)
                {
                    if (TryComp<RCDFAPDeconstructableComponent>(args.Target, out var destructible))
                    {
                        cost = destructible.Cost;
                        delay = destructible.Delay;
                        effectPrototype = destructible.Effect;
                    }
                }

                // Deconstructing a tile
                else
                {
                    var deconstructedTile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
                    var protoName = !_turf.IsSpace(deconstructedTile) ? _deconstructTileProto : _deconstructLatticeProto;

                    if (_protoManager.Resolve(protoName, out var deconProto))
                    {
                        cost = deconProto.Cost;
                        delay = deconProto.Delay;
                        effectPrototype = deconProto.Effect;
                    }
                }

                break;

            case RcdfapMode.ConstructTile:

                // If replacing a tile, make the construction instant
                var contructedTile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);

                if (!contructedTile.Tile.IsEmpty)
                {
                    delay = _instantConstructionDelay;
                    effectPrototype = effectPrototype;
                }

                break;
        }

        #endregion

        // Try to start the do after
        var effect = Spawn(effectPrototype, _mapSystem.ToCenterCoordinates(tile, mapGrid));
        var ev = new RCDFAPDoAfterEvent(
            GetNetCoordinates(location),
            GetNetEntity(gridUid.Value),
            component.ConstructionDirection,
            component.ProtoId,
            cost,
            GetNetEntity(effect));

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, ev, uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = false,
            BlockDuplicate = false
        };

        args.Handled = true;

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            QueueDel(effect);
    }

    private void OnDoAfterAttempt(EntityUid uid, RCDFAPComponent component, DoAfterAttemptEvent<RCDFAPDoAfterEvent> args)
    {
        if (args.Event?.DoAfter?.Args == null)
            return;

        // Exit if the RCDFAP prototype has changed
        if (component.ProtoId != args.Event.StartingProtoId)
        {
            args.Cancel();
            return;
        }

        // Ensure the RCDFAP operation is still valid
        var gridUid = GetEntity(args.Event.TargetGridId);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            args.Cancel();
            return;
        }

        var location = GetCoordinates(args.Event.Location);
        var tile = _mapSystem.GetTileRef(gridUid, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid, mapGrid, location);

        if (!IsRCDFAPOperationStillValid(uid, component, gridUid, mapGrid, tile, position, args.Event.Direction, args.Event.Target, args.Event.User))
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, RCDFAPComponent component, RCDFAPDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            // Delete the effect entity if the do-after was cancelled (server-side only)
            if (_net.IsServer)
                QueueDel(GetEntity(args.Effect));
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        var gridUid = GetEntity(args.TargetGridId);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var location = GetCoordinates(args.Location);
        var tile = _mapSystem.GetTileRef(gridUid, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid, mapGrid, location);

        // Ensure the RCDFAP operation is still valid
        if (!IsRCDFAPOperationStillValid(uid, component, gridUid, mapGrid, tile, position, args.Direction, args.Target, args.User))
        {
            return;
        }

        // Finalize the operation (this should handle prediction properly)
        FinalizeRCDFAPOperation(uid, component, gridUid, mapGrid, tile, position, args.Direction, args.Target, args.User);

        // Play audio and consume charges
        _audio.PlayPredicted(component.SuccessSound, uid, args.User);
        _sharedCharges.AddCharges(uid, -args.Cost);
    }

    private void OnRCDFAPconstructionGhostRotationEvent(RCDFAPConstructionGhostRotationEvent ev, EntitySessionEventArgs session)
    {
        var uid = GetEntity(ev.NetEntity);

        // Determine if player that send the message is carrying the specified RCDFAP in their active hand
        if (session.SenderSession.AttachedEntity is not { } player)
            return;

        if (_hands.GetActiveItem(player) != uid)
            return;

        if (!TryComp<RCDFAPComponent>(uid, out var rcdfap))
            return;

        // Update the construction direction
        rcdfap.ConstructionDirection = ev.Direction;
        Dirty(uid, rcdfap);
    }

    #endregion

    #region Entity construction/deconstruction rule checks

    public bool IsRCDFAPOperationStillValid(EntityUid uid, RCDFAPComponent component, EntityUid gridUid, MapGridComponent mapGrid, TileRef tile, Vector2i position, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        return IsRCDFAPOperationStillValid(uid, component, gridUid, mapGrid, tile, position, component.ConstructionDirection, target, user, popMsgs);
    }

    public bool IsRCDFAPOperationStillValid(EntityUid uid, RCDFAPComponent component, EntityUid gridUid, MapGridComponent mapGrid, TileRef tile, Vector2i position, Direction direction, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        var prototype = _protoManager.Index(component.ProtoId);

        // Check that the RCDFAP has enough ammo to get the job done
        var charges = _sharedCharges.GetCurrentCharges(uid);

        // Both of these were messages were suppose to be predicted, but HasInsufficientCharges wasn't being checked on the client for some reason?
        if (charges == 0)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcdfap-component-no-ammo-message"), uid, user);

            return false;
        }

        if (prototype.Cost > charges)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcdfap-component-insufficient-ammo-message"), uid, user);

            return false;
        }

        // Exit if the target / target location is obstructed
        var unobstructed = (target == null)
            ? _interaction.InRangeUnobstructed(user, _mapSystem.GridTileToWorld(gridUid, mapGrid, position), popup: popMsgs)
            : _interaction.InRangeUnobstructed(user, target.Value, popup: popMsgs);

        if (!unobstructed)
            return false;

        // Return whether the operation location is valid
        switch (prototype.Mode)
        {
            case RcdfapMode.ConstructTile:
            case RcdfapMode.ConstructObject:
                return IsConstructionLocationValid(uid, component, gridUid, mapGrid, tile, position, direction, user, popMsgs);
            case RcdfapMode.Deconstruct:
                return IsDeconstructionStillValid(uid, tile, target, user, popMsgs);
        }

        return false;
    }

    private bool IsConstructionLocationValid(EntityUid uid, RCDFAPComponent component, EntityUid gridUid, MapGridComponent mapGrid, TileRef tile, Vector2i position, Direction direction, EntityUid user, bool popMsgs = true)
    {
        var prototype = _protoManager.Index(component.ProtoId);

        // Check rule: Must build on empty tile
        if (prototype.ConstructionRules.Contains(RcdfapConstructionRule.MustBuildOnEmptyTile) && !tile.Tile.IsEmpty)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcdfap-component-must-build-on-empty-tile-message"), uid, user);

            return false;
        }

        // Check rule: Must build on non-empty tile
        if (!prototype.ConstructionRules.Contains(RcdfapConstructionRule.CanBuildOnEmptyTile) && tile.Tile.IsEmpty)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcdfap-component-cannot-build-on-empty-tile-message"), uid, user);

            return false;
        }

        // Check rule: Must place on subfloor
        if (prototype.ConstructionRules.Contains(RcdfapConstructionRule.MustBuildOnSubfloor) && !_turf.GetContentTileDefinition(tile).IsSubFloor)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcdfap-component-must-build-on-subfloor-message"), uid, user);

            return false;
        }

        // Tile specific rules
        if (prototype.Mode == RcdfapMode.ConstructTile)
        {
            // Check rule: Tile placement is valid
            if (!_floors.CanPlaceTile(gridUid, mapGrid, tile.GridIndices, out var reason))
            {
                if (popMsgs)
                    _popup.PopupClient(reason, uid, user);

                return false;
            }

            var tileDef = _turf.GetContentTileDefinition(tile);

            // Check rule: Respect baseTurf and baseWhitelist
            if (prototype.Prototype != null && _tileDefMan.TryGetDefinition(prototype.Prototype, out var replacementDef))
            {
                var replacementContentDef = (ContentTileDefinition) replacementDef;

                if (replacementContentDef.BaseTurf != tileDef.ID && !replacementContentDef.BaseWhitelist.Contains(tileDef.ID))
                {
                    if (popMsgs)
                        _popup.PopupClient(Loc.GetString("rcdfap-component-cannot-build-on-empty-tile-message"), uid, user);

                    return false;
                }
            }

            // Check rule: Tiles can't be identical
            if (tileDef.ID == prototype.Prototype)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcdfap-component-cannot-build-identical-tile"), uid, user);

                return false;
            }

            // Ensure that all construction rules shared between tiles and object are checked before exiting here
            return true;
        }

        // Entity specific rules

        // Check rule: The tile is unoccupied
        var isWindow = prototype.ConstructionRules.Contains(RcdfapConstructionRule.IsWindow);
        var isCatwalk = prototype.ConstructionRules.Contains(RcdfapConstructionRule.IsCatwalk);
        var isWall = prototype.ConstructionRules.Contains(RcdfapConstructionRule.IsWall);

        _intersectingEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, position, _intersectingEntities, -0.05f, LookupFlags.Uncontained);

        foreach (var ent in _intersectingEntities)
        {
            // If the entity is the exact same prototype as what we are trying to build, then block it.
            // This is to prevent spamming objects on the same tile (e.g. lights)
            if (prototype.Prototype != null && MetaData(ent).EntityPrototype?.ID == prototype.Prototype)
            {
                var isIdentical = true;

                if (prototype.AllowMultiDirection)
                {
                    var entDirection = Transform(ent).LocalRotation.GetCardinalDir();
                    if (entDirection != direction)
                        isIdentical = false;
                }

                if (isIdentical)
                {
                    if (popMsgs)
                        _popup.PopupClient(Loc.GetString("rcdfap-component-cannot-build-identical-entity"), uid, user);

                    return false;
                }
            }

            if (isWindow && HasComp<SharedCanBuildWindowOnTopRCDFAPComponent>(ent))
                continue;

            if (isCatwalk && _tags.HasTag(ent, CatwalkTag))
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcdfap-component-cannot-build-on-occupied-tile-message"), uid, user);

                return false;
            }

            if (isWall && HasComp<SharedCanBuildWallOnTopRCDFAPComponent>(ent))
                continue;

            if (prototype.CollisionMask != CollisionGroup.None && TryComp<FixturesComponent>(ent, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    // Continue if no collision is possible
                    if (!fixture.Hard || fixture.CollisionLayer <= 0 || (fixture.CollisionLayer & (int) prototype.CollisionMask) == 0)
                        continue;

                    // Continue if our custom collision bounds are not intersected
                    if (prototype.CollisionPolygon != null &&
                        !DoesCustomBoundsIntersectWithFixture(prototype.CollisionPolygon, component.ConstructionTransform, ent, fixture))
                        continue;

                    // Collision was detected
                    if (popMsgs)
                        _popup.PopupClient(Loc.GetString("rcdfap-component-cannot-build-on-occupied-tile-message"), uid, user);

                    return false;
                }
            }
        }

        return true;
    }

    private bool IsDeconstructionStillValid(EntityUid uid, TileRef tile, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        // Attempt to deconstruct a floor tile
        if (target == null)
        {
            // The tile is empty
            if (tile.Tile.IsEmpty)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcdfap-component-nothing-to-deconstruct-message"), uid, user);

                return false;
            }

            // The tile has a structure sitting on it
            if (_turf.IsTileBlocked(tile, CollisionGroup.MobMask))
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcdfap-component-tile-obstructed-message"), uid, user);

                return false;
            }

            // The tile cannot be destroyed
            var tileDef = _turf.GetContentTileDefinition(tile);

            if (tileDef.Indestructible)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcdfap-component-tile-indestructible-message"), uid, user);

                return false;
            }
        }

        // Attempt to deconstruct an object
        else
        {
            // The object is not in the whitelist
            if (!TryComp<RCDFAPDeconstructableComponent>(target, out var deconstructible) || !deconstructible.Deconstructable)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rcdfap-component-deconstruct-target-not-on-whitelist-message"), uid, user);

                return false;
            }
        }

        return true;
    }

    #endregion

    #region Entity construction/deconstruction

    private void FinalizeRCDFAPOperation(EntityUid uid, RCDFAPComponent component, EntityUid gridUid, MapGridComponent mapGrid, TileRef tile, Vector2i position, Direction direction, EntityUid? target, EntityUid user)
    {
        if (!_net.IsServer)
            return;

        var prototype = _protoManager.Index(component.ProtoId);

        if (prototype.Prototype == null)
            return;

        switch (prototype.Mode)
        {
            case RcdfapMode.ConstructTile:
                if (!_tileDefMan.TryGetDefinition(prototype.Prototype, out var tileDef))
                    return;

                _tile.ReplaceTile(tile, (ContentTileDefinition) tileDef, gridUid, mapGrid);
                _adminLogger.Add(LogType.RCDFAP, LogImpact.High, $"{ToPrettyString(user):user} used RCDFAP to set grid: {gridUid} {position} to {prototype.Prototype}");
                break;

            case RcdfapMode.ConstructObject:
                var ent = Spawn(prototype.Prototype, _mapSystem.GridTileToLocal(gridUid, mapGrid, position));

                switch (prototype.Rotation)
                {
                    case RcdfapRotation.Fixed:
                        Transform(ent).LocalRotation = Angle.Zero;
                        break;
                    case RcdfapRotation.Camera:
                        Transform(ent).LocalRotation = Transform(uid).LocalRotation;
                        break;
                    case RcdfapRotation.User:
                        Transform(ent).LocalRotation = direction.ToAngle();
                        break;
                }

                _adminLogger.Add(LogType.RCDFAP, LogImpact.High, $"{ToPrettyString(user):user} used RCDFAP to spawn {ToPrettyString(ent)} at {position} on grid {gridUid}");
                break;

            case RcdfapMode.Deconstruct:

                if (target == null)
                {
                    // Deconstruct tile, don't drop tile as item
                    if (_tile.DeconstructTile(tile, spawnItem: false))
                        _adminLogger.Add(LogType.RCDFAP, LogImpact.High, $"{ToPrettyString(user):user} used RCDFAP to set grid: {gridUid} tile: {position} open to space");
                }
                else
                {
                    // Deconstruct object
                    _adminLogger.Add(LogType.RCDFAP, LogImpact.High, $"{ToPrettyString(user):user} used RCDFAP to delete {ToPrettyString(target):target}");
                    QueueDel(target);
                }

                break;
        }
    }

    #endregion

    #region Utility functions

    private bool DoesCustomBoundsIntersectWithFixture(PolygonShape boundingPolygon, Transform boundingTransform, EntityUid fixtureOwner, Fixture fixture)
    {
        var entXformComp = Transform(fixtureOwner);
        var entXform = new Transform(new(), entXformComp.LocalRotation);

        return boundingPolygon.ComputeAABB(boundingTransform, 0).Intersects(fixture.Shape.ComputeAABB(entXform, 0));
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class RCDFAPDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Location { get; private set; }

    [DataField(required: true)]
    public NetEntity TargetGridId {get ; private set; }

    [DataField]
    public Direction Direction { get; private set; }

    [DataField]
    public ProtoId<RCDFAPPrototype> StartingProtoId { get; private set; }

    [DataField]
    public int Cost { get; private set; } = 1;

    [DataField("fx")]
    public NetEntity? Effect { get; private set; }

    private RCDFAPDoAfterEvent() { }

    public RCDFAPDoAfterEvent(
        NetCoordinates location,
        NetEntity targetGridId,
        Direction direction,
        ProtoId<RCDFAPPrototype>
        startingProtoId,
        int cost,
        NetEntity? effect = null)
    {
        Location = location;
        TargetGridId = targetGridId;
        Direction = direction;
        StartingProtoId = startingProtoId;
        Cost = cost;
        Effect = effect;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
