using Content.Server.Medical.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Server.Body.Systems;
using Content.Shared.Mobs; // LP Edit
using Content.Shared._LP.HealthAnalyzer; // LP Edit

namespace Content.Server.Medical;

public sealed class HealthAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HealthAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HealthAnalyzerComponent, HealthAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HealthAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<HealthAnalyzerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<HealthAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<HealthAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            //Update rate limited to 1 second
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.ScannedEntity is not {} patient)
                continue;

            if (Deleted(patient))
            {
                StopAnalyzingEntity((uid, component), patient);
                continue;
            }

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            //Get distance between health analyzer and the scanned entity
            //null is infinite range
            var patientCoordinates = Transform(patient).Coordinates;
            if (component.MaxScanRange != null && !_transformSystem.InRange(patientCoordinates, transform.Coordinates, component.MaxScanRange.Value))
            {
                //Range too far, disable updates until they are back in range
                PauseAnalyzingEntity((uid, component), patient);
                continue;
            }

            component.IsAnalyzerActive = true;
            UpdateScannedUser(uid, patient, true);
        }
    }

    /// <summary>
    /// Trigger the doafter for scanning
    /// </summary>
    private void OnAfterInteract(Entity<HealthAnalyzerComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<MobStateComponent>(args.Target) || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        _audio.PlayPvs(uid.Comp.ScanningBeginSound, uid);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.ScanDelay, new HealthAnalyzerDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled || uid.Comp.Silent)
            return;

        var msg = Loc.GetString("health-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popupSystem.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    private void OnDoAfter(Entity<HealthAnalyzerComponent> uid, ref HealthAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(uid.Owner, user: args.User))
            return;

        if (!uid.Comp.Silent)
            _audio.PlayPvs(uid.Comp.ScanningEndSound, uid);

        OpenUserInterface(args.User, uid);
        // LP Edit Start
        BeginAnalyzingEntity(uid, args.Target.Value, args.User);
        var uiState = GetHealthAnalyzerUiState(args.Target.Value);

        RaiseNetworkEvent(
            new HealthAnalyzerAudioEvent(
                uiState.MobState ?? MobState.Alive,
                true),
            args.User);
        args.Handled = true;
        // LP Edit End
    }

    /// <summary>
    /// Turn off when placed into a storage item or moved between slots/hands
    /// </summary>
    private void OnInsertedIntoContainer(Entity<HealthAnalyzerComponent> uid, ref EntGotInsertedIntoContainerMessage args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);

        // LP Edit Start

        if (uid.Comp.ScannerUser != null)
        {
            RaiseNetworkEvent(
                new HealthAnalyzerStopAudioEvent(),
                uid.Comp.ScannerUser.Value);
        }

        // LP Edit End

    }

    /// <summary>
    /// Disable continuous updates once turned off
    /// </summary>
    private void OnToggled(Entity<HealthAnalyzerComponent> ent, ref ItemToggledEvent args)
    {
        if (!args.Activated && ent.Comp.ScannedEntity is { } patient)
            StopAnalyzingEntity(ent, patient);

        // LP Edit Start

        if (ent.Comp.ScannerUser != null)
        {
            RaiseNetworkEvent(
                new HealthAnalyzerStopAudioEvent(),
                ent.Comp.ScannerUser.Value);
        }

        // LP Edit End

    }

    /// <summary>
    /// Turn off the analyser when dropped
    /// </summary>
    private void OnDropped(Entity<HealthAnalyzerComponent> uid, ref DroppedEvent args)
    {
        if (uid.Comp.ScannedEntity is { } patient)
            _toggle.TryDeactivate(uid.Owner);

        // LP Edit Start

        if (uid.Comp.ScannerUser != null)
        {
            RaiseNetworkEvent(
                new HealthAnalyzerStopAudioEvent(),
                uid.Comp.ScannerUser.Value);
        }

        // LP Edit End

    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, HealthAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, HealthAnalyzerUiKey.Key, user);
    }

    /// <summary>
    /// Mark the entity as having its health analyzed, and link the analyzer to it
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that should receive the updates</param>
    /// <param name="target">The entity to start analyzing</param>
    /// <param name="user">The user operating the analyzer</param> // LP Edit
    private void BeginAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target, EntityUid user) // LP Edit
    {
        //Link the health analyzer to the scanned entity
        healthAnalyzer.Comp.ScannedEntity = target;
        healthAnalyzer.Comp.ScannerUser = user; // LP Edit

        _toggle.TryActivate(healthAnalyzer.Owner);

        UpdateScannedUser(healthAnalyzer, target, true);
    }

    /// <summary>
    /// Remove the analyzer from the active list, and remove the component if it has no active analyzers
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void StopAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {
        var scannerUser = healthAnalyzer.Comp.ScannerUser; // LP Edit

        //Unlink the analyzer
        healthAnalyzer.Comp.ScannedEntity = null;

        // LP Edit Start

        healthAnalyzer.Comp.ScannerUser = null;

        if (scannerUser != null)
        {
            RaiseNetworkEvent(
                new HealthAnalyzerStopAudioEvent(),
                scannerUser.Value);
        }

        // LP Edit End

        _toggle.TryDeactivate(healthAnalyzer.Owner);

        UpdateScannedUser(healthAnalyzer, target, false);
    }


    /// <summary>
    /// If the scanner is active, sends one last update and sets it to inactive.
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer that's receiving the updates</param>
    /// <param name="target">The entity to analyze</param>
    private void PauseAnalyzingEntity(Entity<HealthAnalyzerComponent> healthAnalyzer, EntityUid target)
    {

        // LP Edit Start

        if (healthAnalyzer.Comp.ScannerUser != null)
        {
            RaiseNetworkEvent(
                new HealthAnalyzerStopAudioEvent(),
                healthAnalyzer.Comp.ScannerUser.Value);
        }

        // LP Edit End

        if (!healthAnalyzer.Comp.IsAnalyzerActive)
            return;

        UpdateScannedUser(healthAnalyzer, target, false);
        healthAnalyzer.Comp.IsAnalyzerActive = false;
    }

    /// <summary>
    /// Send an update for the target to the healthAnalyzer
    /// </summary>
    /// <param name="healthAnalyzer">The health analyzer</param>
    /// <param name="target">The entity being scanned</param>
    /// <param name="scanMode">True makes the UI show ACTIVE, False makes the UI show INACTIVE</param>
    public void UpdateScannedUser(EntityUid healthAnalyzer, EntityUid target, bool scanMode)
    {
        if (!_uiSystem.HasUi(healthAnalyzer, HealthAnalyzerUiKey.Key)
            || !HasComp<DamageableComponent>(target))
            return;

        var uiState = GetHealthAnalyzerUiState(target);
        uiState.ScanMode = scanMode;

        _uiSystem.ServerSendUiMessage(
            healthAnalyzer,
            HealthAnalyzerUiKey.Key,
            new HealthAnalyzerScannedUserMessage(uiState)
        );

        // LP Edit Start

        if (TryComp<HealthAnalyzerComponent>(healthAnalyzer, out var analyzer) && analyzer.ScannerUser != null)
        {
            RaiseNetworkEvent(new HealthAnalyzerAudioEvent(uiState.MobState ?? MobState.Alive), analyzer.ScannerUser.Value);
        }

        // LP Edit End

    }

    /// <summary>
    /// Creates a HealthAnalyzerState based on the current state of an entity.
    /// </summary>
    /// <param name="target">The entity being scanned</param>
    /// <returns></returns>
    public HealthAnalyzerUiState GetHealthAnalyzerUiState(EntityUid? target)
    {
        if (!target.HasValue || !HasComp<DamageableComponent>(target))
            return new HealthAnalyzerUiState();

        var entity = target.Value;
        var bodyTemperature = float.NaN;

        if (TryComp<TemperatureComponent>(entity, out var temp))
            bodyTemperature = temp.CurrentTemperature;

        var bloodAmount = float.NaN;
        var bleeding = false;
        var unrevivable = false;
        var mobState = MobState.Alive; // LP Edit

        if (TryComp<BloodstreamComponent>(entity, out var bloodstream) &&
            _solutionContainerSystem.ResolveSolution(entity, bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution, out var bloodSolution))
        {
            bloodAmount = _bloodstreamSystem.GetBloodLevel(entity);
            bleeding = bloodstream.BleedAmount > 0;
        }

        if (TryComp<UnrevivableComponent>(entity, out var unrevivableComp) && unrevivableComp.Analyzable)
            unrevivable = true;

        // LP Edit Start
        if (TryComp<MobStateComponent>(target, out var mob))
            mobState = mob.CurrentState;
        // LP Edit End

        return new HealthAnalyzerUiState(
            GetNetEntity(entity),
            bodyTemperature,
            bloodAmount,
            null,
            bleeding,
            unrevivable,
            mobState // LP Edit
        );
    }
}
