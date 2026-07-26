using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using System.Linq; // Goob edit
using Content.Shared.Popups; // Goob edit
using Content.Shared.Whitelist; // Goob edit

namespace Content.Shared.Pinpointer;

public abstract class SharedPinpointerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] protected readonly EntityWhitelistSystem Whitelist = default!; // Goob edit
    [Dependency] private readonly SharedPopupSystem _popup = default!; // Goob edit

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PinpointerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<PinpointerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PinpointerComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    ///     Set the target if capable
    /// </summary>
    private void OnAfterInteract(EntityUid uid, PinpointerComponent component, AfterInteractEvent args) // Goob edit
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        if (!component.CanRetarget || component.IsActive) // Goob edit
            return;

        // Goob edit start: retargeting has a whitelist
        args.Handled = true;

        if (Whitelist.IsWhitelistFail(component.RetargetingWhitelist, target) ||
            Whitelist.IsBlacklistPass(component.RetargetingBlacklist, target))
        {
            return;
        }

        // TODO add doafter once the freeze is lifted
        // ignore can target multiple, because too hard to support
        component.Targets.Clear();
        component.Targets.Add(target);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} set target of {ToPrettyString(uid):pinpointer} to {ToPrettyString(target):target}");
        if (component.UpdateTargetName)
            component.TargetName = Identity.Name(target, EntityManager);

        _popup.PopupPredicted(Loc.GetString("pinpointer-link-success"), uid, args.User);
        // Goob edit end
    }

    /// <summary>
    ///     Set pinpointers target to track
    ///     Goob edit: If CanTargetMultiple is true in Pinpointer component, then it will be ADDED, not set
    /// </summary>
    public virtual void SetTarget(EntityUid uid, EntityUid? target, PinpointerComponent? pinpointer = null) // Goob edit
    {
        if (!Resolve(uid, ref pinpointer)) // Goob edit
            return;

        // Goob edit start
        if (target == null || pinpointer.Targets.Contains(target.Value))
        {
            return;
        }

        if (!pinpointer.CanTargetMultiple)
        {
            pinpointer.Targets.Clear();
        }

        if (TerminatingOrDeleted(target.Value))
        {
            TrySetArrowAngle((uid, pinpointer), Angle.Zero);
            return;
        }

        pinpointer.Targets.Add(target.Value);

        if (pinpointer.UpdateTargetName)
            pinpointer.TargetName = Identity.Name(target.Value, EntityManager);
        // WD EDIT START - UpdateDirectionToTarget is triggered when updating, no need to run it again
        // if (pinpointer.IsActive)
        //    UpdateDirectionToTarget(uid, pinpointer);
        // WD EDIT END
    }

    /// <summary>
    /// Goob edit: sets a list of targets for a pinpointer.
    /// </summary>
    public virtual void SetTargets(EntityUid uid, List<EntityUid> targets, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (!pinpointer.CanTargetMultiple)
        {
            return; // No.
        }

        var targetsList = targets.Where(Exists).ToList();

        pinpointer.Targets = targetsList;

        // WD EDIT START - UpdateDirectionToTarget is triggered when updating, no need to run it again
        // if (pinpointer.IsActive)
        //    UpdateDirectionToTarget(uid, pinpointer);
        // WD EDIT END
    }
    // Goob edit end

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected virtual void UpdateDirectionToTarget(Entity<PinpointerComponent?> ent)
    {

    }

    private void OnExamined(Entity<PinpointerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.TargetName == null)
            return;

        args.PushMarkup(Loc.GetString("examine-pinpointer-linked", ("target", ent.Comp.TargetName)));
    }

    /// <summary>
    ///     Manually set distance from pinpointer to target
    /// </summary>
    public void SetDistance(Entity<PinpointerComponent?> ent, Distance distance)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (distance == ent.Comp.DistanceToTarget)
            return;

        ent.Comp.DistanceToTarget = distance;
        Dirty(ent);
    }

    /// <summary>
    ///     Try to manually set pinpointer arrow direction.
    ///     If difference between current angle and new angle is smaller than
    ///     pinpointer precision, new value will be ignored and it will return false.
    /// </summary>
    public bool TrySetArrowAngle(Entity<PinpointerComponent?> ent, Angle arrowAngle)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.ArrowAngle.EqualsApprox(arrowAngle, ent.Comp.Precision))
            return false;

        ent.Comp.ArrowAngle = arrowAngle;
        Dirty(ent);

        return true;
    }

    /// <summary>
    ///     Activate/deactivate pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    public void SetActive(Entity<PinpointerComponent?> ent, bool isActive)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (isActive == ent.Comp.IsActive)
            return;

        ent.Comp.IsActive = isActive;
        Dirty(ent);
    }


    /// <summary>
    ///     Toggle Pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    /// <returns>True if pinpointer was activated, false otherwise</returns>
    public virtual bool TogglePinpointer(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var isActive = !ent.Comp.IsActive;
        SetActive(ent, isActive);
        return isActive;
    }

    private void OnEmagged(Entity<PinpointerComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        if (ent.Comp.CanRetarget)
            return;

        args.Handled = true;
        ent.Comp.CanRetarget = true;
    }
}
