using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using System.Linq;
using System.Numerics;
using Robust.Shared.Utility;
using Content.Server.Shuttles.Events;
using Content.Shared.Whitelist; // Goobstation edit

namespace Content.Server.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<PinpointerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<FTLCompletedEvent>(OnLocateTarget);
    }

    public override bool TogglePinpointer(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var isActive = !ent.Comp.IsActive;
        SetActive(ent, isActive);
        UpdateAppearance(ent);
        return isActive;
    }

    private void UpdateAppearance(Entity<PinpointerComponent?, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1) || !Resolve(ent, ref ent.Comp2))
            return;

        _appearance.SetData(ent, PinpointerVisuals.IsActive, ent.Comp1.IsActive, ent.Comp2);
        _appearance.SetData(ent, PinpointerVisuals.TargetDistance, ent.Comp1.DistanceToTarget, ent.Comp2);
    }

    private void OnActivate(Entity<PinpointerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        TogglePinpointer(ent.AsNullable());

        if (!ent.Comp.CanRetarget)
            LocateTarget(ent.Owner, ent.Comp); // Goobstation edit

        args.Handled = true;
    }

    private void OnLocateTarget(ref FTLCompletedEvent ev)
    {
        // This feels kind of expensive, but it only happens once per hyperspace jump

        // todo: ideally, you would need to raise this event only on jumped entities
        // this code update ALL pinpointers in game
        // Goob edit start: tracking Xform and checking that pinpointer is the jumped one
        var query = EntityQueryEnumerator<PinpointerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var pinpointer, out var transform))
        {
            if (pinpointer.CanRetarget)
                continue;

            if (transform.GridUid != ev.Entity)
                continue;

            LocateTarget(uid, pinpointer);
        }
    }

    /// <summary>
    /// Goob edit: this was literally fully changed. But still works as intended
    /// </summary>
    private void LocateTarget(EntityUid uid, PinpointerComponent component)
    {
        if (!component.IsActive || component.Whitelist == null)
            return;

        if (component.CanTargetMultiple)
        {
            var targets = FindAllTargetsFromComponent(uid, component.Whitelist, component.Blacklist);
            SetTargets(uid, targets, component);
        }
        else
        {
            var target = FindTargetFromComponent(uid, component.Whitelist, component.Blacklist);
            SetTarget(uid, target, component);
        }
    }
    // Goob edit end

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // because target or pinpointer can move
        // we need to update pinpointers arrow each frame
        var query = EntityQueryEnumerator<PinpointerComponent>();
        while (query.MoveNext(out var uid, out var pinpointer))
        {
            UpdateDirectionToTarget((uid, pinpointer));
        }
    }

    // Goob edit start

    /// <summary>
    ///     Try to find the closest entity from whitelist on a current map
    ///     Will return null if can't find anything
    ///     Goob edit: requires EntityWhitelist instead of just Type.
    /// </summary>
    private EntityUid? FindTargetFromComponent(
        Entity<TransformComponent?> ent,
        EntityWhitelist whitelist,
        EntityWhitelist? blacklist)
    {
        _xformQuery.Resolve(ent, ref ent.Comp, false);

        if (ent.Comp == null)
            return null;

        var transform = ent.Comp;

        // sort all entities in distance increasing order
        var mapId = transform.MapID;
        var l = new SortedList<float, EntityUid>();
        var worldPos = _transform.GetWorldPosition(transform);

        if (whitelist.Components == null)
            return null;

        foreach (var component in whitelist.Components)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(component, out var reg))
            {
                Log.Error($"Unable to find component registration for {component} for pinpointer!");
                DebugTools.Assert(false);
                return null;
            }

            foreach (var (otherUid, _) in EntityManager.GetAllComponents(reg.Type))
            {
                if (!_xformQuery.TryGetComponent(otherUid, out var compXform) || compXform.MapID != mapId)
                    continue;

                if (Whitelist.IsBlacklistPass(blacklist, otherUid))
                    continue;

                var dist = (_transform.GetWorldPosition(compXform) - worldPos).LengthSquared();
                l.TryAdd(dist, otherUid);
            }
        }
        // Goob edit end

        // return uid with a smallest distance
        return l.Count > 0 ? l.First().Value : null;
    }

    // Goob edit start

    /// <summary>
    /// Goob edit: Gets all possible targets within it's whitelist relative to pinpointer entity.
    /// </summary>
    private List<EntityUid> FindAllTargetsFromComponent(
        Entity<TransformComponent?> ent,
        EntityWhitelist whitelist,
        EntityWhitelist? blacklist)
    {
        _xformQuery.Resolve(ent, ref ent.Comp, false);
        var list = new List<EntityUid>();

        if (ent.Comp == null)
            return list;

        var transform = ent.Comp;
        var mapId = transform.MapID;

        if (whitelist.Components == null)
            return list;

        foreach (var component in whitelist.Components)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(component, out var reg))
            {
                Log.Error($"Unable to find component registration for {component} for pinpointer!");
                DebugTools.Assert(false);
                return list;
            }

            foreach (var (otherUid, _) in EntityManager.GetAllComponents(reg.Type))
            {
                if (!_xformQuery.TryGetComponent(otherUid, out var compXform) || compXform.MapID != mapId)
                    continue;

                if (Whitelist.IsBlacklistPass(blacklist, otherUid))
                    continue;

                list.Add(otherUid);
            }
        }

        return list;
    }

    // Goob edit end

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected override void UpdateDirectionToTarget(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var pinpointer = ent.Comp;

        if (!pinpointer.IsActive)
            return;

        var target = GetNearestTarget((ent.Owner, pinpointer)); // Goob edit
        if (target == null || !Exists(target.Value))
        {
            SetDistance(ent, Distance.Unknown);
            return;
        }

        var dirVec = CalculateDirection(ent, target.Value);
        var oldDist = pinpointer.DistanceToTarget;
        if (dirVec != null)
        {
            var angle = dirVec.Value.ToWorldAngle();
            TrySetArrowAngle(ent, angle);
            var dist = CalculateDistance(dirVec.Value, pinpointer);
            SetDistance(ent, dist);
        }
        else
        {
            SetDistance(ent, Distance.Unknown);
        }
        if (oldDist != pinpointer.DistanceToTarget)
            UpdateAppearance(ent);
    }

    /// <summary>
    ///     Calculate direction from pinUid to trgUid
    /// </summary>
    /// <returns>Null if failed to calculate distance between two entities</returns>
    private Vector2? CalculateDirection(EntityUid pinUid, EntityUid trgUid)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        // check if entities have transform component
        if (!xformQuery.TryGetComponent(pinUid, out var pin))
            return null;
        if (!xformQuery.TryGetComponent(trgUid, out var trg))
            return null;

        // check if they are on same map
        if (pin.MapID != trg.MapID)
            return null;

        // get world direction vector
        var dir = _transform.GetWorldPosition(trg, xformQuery) - _transform.GetWorldPosition(pin, xformQuery);
        return dir;
    }

    // Goob edit start

    /// <summary>
    /// Goob edit: gets the nearest target out of pinpointer's Targets list.
    /// </summary>
    private EntityUid? GetNearestTarget(Entity<PinpointerComponent> ent)
    {
        var list = new SortedList<float, EntityUid>();
        foreach (var target in ent.Comp.Targets)
        {
            var lengh = CalculateDirection(ent, target);
            if (lengh == null)
                continue;

            var dist = lengh.Value.Length();
            if (!list.TryAdd(dist, target))
                list.TryAdd(dist + 1f, target); // safety measure
        }

        return list.Count > 0 ? list.First().Value : null;
    }

    // Goob edit end

    private Distance CalculateDistance(Vector2 vec, PinpointerComponent pinpointer)
    {
        var dist = vec.Length();
        if (dist <= pinpointer.ReachedDistance)
            return Distance.Reached;
        else if (dist <= pinpointer.CloseDistance)
            return Distance.Close;
        else if (dist <= pinpointer.MediumDistance)
            return Distance.Medium;
        else
            return Distance.Far;
    }
}
