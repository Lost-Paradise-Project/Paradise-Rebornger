using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Research.Disk;
using Content.Server.Stack;
using Content.Shared.Audio;
using Content.Shared._Wega.Mining;
using Content.Shared._Wega.Mining.Components;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
// LP edit start
using Content.Shared._LP.Mining.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
// LP edit end

namespace Content.Server._Wega.Mining;

public sealed class MiningConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    // LP edit start
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    private static readonly EntProtoId MiningDisk = "LPPMiningResearchDisk";

    // Auto-update interval in seconds
    private const float AutoUpdateInterval = 2.0f;
    private float _updateTimer;
    // LP edit end
    private static readonly ProtoId<StackPrototype> Credit = "Credit";
    /* private static readonly EntProtoId Disk = "ResearchDisk"; */ // LP edit (mining system)

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiningConsoleComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<MiningConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);

        // LP edit start
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleActivateAllMessage>(OnActivateAll);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleDeactivateAllMessage>(OnDeactivateAll);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleToggleServerActivationMessage>(OnToggleServerActivation);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleChangeServerStageMessage>(OnChangeServerStage);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleToggleModeMessage>(OnToggleMode);
        // LP edit end

        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleWithdrawMessage>(OnWithdraw);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleSetAllStagesMessage>(OnSetAllStages);
    }

    // LP edit start
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTimer += frameTime;
        if (_updateTimer >= AutoUpdateInterval)
        {
            _updateTimer = 0;
            UpdateAllConsoles();
        }
    }

    private void UpdateAllConsoles()
    {
        var query = EntityQueryEnumerator<MiningConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, MiningConsoleUiKey.Key))
            {
                UpdateUi((uid, comp));
            }
        }
    }
    // LP edit end

    private void OnInit(EntityUid uid, MiningConsoleComponent comp, MapInitEvent args)
    {
        comp.LinkedServer = EnsureAccount();
    }

    private void OnUIOpened(Entity<MiningConsoleComponent> entity, ref BoundUIOpenedEvent args)
    {
        UpdateUi(entity);
    }

    // LP edit start
    private void OnActivateAll(Entity<MiningConsoleComponent> entity, ref MiningConsoleActivateAllMessage args)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        SetGlobalActivation(entity.Owner, true);
    }

    private void OnDeactivateAll(Entity<MiningConsoleComponent> entity, ref MiningConsoleDeactivateAllMessage args)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        SetGlobalActivation(entity.Owner, false);
    }
    // LP edit end

    private void OnToggleServerActivation(Entity<MiningConsoleComponent> entity, ref MiningConsoleToggleServerActivationMessage args)
    {
        var serverUid = GetEntity(args.ServerUid);
        ToggleServerActivation(serverUid);
        UpdateUi(entity);
    }

    private void OnToggleMode(Entity<MiningConsoleComponent> entity, ref MiningConsoleToggleModeMessage args)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        var newMode = account.GlobalMode == MiningMode.Credits ? MiningMode.Research : MiningMode.Credits;
        SwitchGlobalMode(entity.Owner, newMode);
        UpdateUi(entity);
    }

    private void OnChangeServerStage(Entity<MiningConsoleComponent> entity, ref MiningConsoleChangeServerStageMessage args)
    {
        var serverUid = GetEntity(args.ServerUid);
        if (TryComp<MiningServerComponent>(serverUid, out var server))
        {
            var newStage = Math.Clamp(server.MiningStage + args.Delta, 1, 3);
            SetServerStage(serverUid, newStage);
            UpdateUi(entity);
        }
    }

    private void OnSetAllStages(Entity<MiningConsoleComponent> entity, ref MiningConsoleSetAllStagesMessage args)
    {
        var target = Math.Clamp(args.Stage, 1, 3);

        var query = EntityQueryEnumerator<MiningServerComponent>();
        while (query.MoveNext(out var serverUid, out var server))
        {
            if (server.MiningStage != target)
                SetServerStage(serverUid, target, server);
        }

        UpdateUi(entity);
    }

    private void OnUpdate(Entity<MiningConsoleComponent> entity, ref MiningConsoleToggleUpdateMessage arg)
        => UpdateUi(entity);

    private void OnWithdraw(Entity<MiningConsoleComponent> entity, ref MiningConsoleWithdrawMessage args)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        if (account.Credits >= 1)
        {
            _stack.SpawnAtPosition((int)account.Credits, Credit, Transform(entity).Coordinates);
            account.Credits = 0;
        }

        if (account.ResearchPoints >= 1)
        {
            // LP edit start
            var points = (int)account.ResearchPoints;
            var disk = Spawn(MiningDisk, Transform(entity).Coordinates);
            var diskComp = EnsureComp<ResearchDiskComponent>(disk);
            diskComp.Points = points;

            // Set dynamic name and description based on points
            var metaData = MetaData(disk);
            _metaData.SetEntityName(disk, Loc.GetString("mining-research-disk-name", ("points", points)), metaData);
            _metaData.SetEntityDescription(disk, Loc.GetString("mining-research-disk-desc", ("points", points)), metaData);
            Dirty(disk, metaData);
            // LP edit end

            account.ResearchPoints = 0;
        }

        UpdateUi(entity);
    }

    public void UpdateUi(Entity<MiningConsoleComponent> entity)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        var servers = new List<MiningServerData>();
        var enabledCount = 0; // LP edit
        var query = EntityQueryEnumerator<MiningServerComponent>();
        while (query.MoveNext(out var serverUid, out var server))
        {
            // LP edit start
            float condition = MiningServerCircuitboardComponent.MaxCondition;
            if (server.CircuitboardUid.HasValue && TryComp<MiningServerCircuitboardComponent>(server.CircuitboardUid.Value, out var board))
            {
                condition = board.Condition;
            }

            if (server.IsActive) // LP edit
                enabledCount++; // LP edit

            servers.Add(new MiningServerData(
                GetNetEntity(serverUid),
                server.MiningStage,
                server.CurrentTemperature,
                server.IsBroken,
                server.IsActive,
                condition
            )); // LP edit (add a condition)
        }

        var state = new MiningConsoleBoundInterfaceState(
            account.Credits,
            account.ResearchPoints,
            account.GlobalMode,
            account.GlobalActivation,
            servers,
            enabledCount // LP edit
        );

        _ui.SetUiState(entity.Owner, MiningConsoleUiKey.Key, state);
    }

    private EntityUid? EnsureAccount()
    {
        var query = EntityQueryEnumerator<MiningAccountComponent>();
        if (query.MoveNext(out var uid, out _))
            return uid;

        return null;
    }

    public void SwitchGlobalMode(EntityUid console, MiningMode mode)
    {
        if (!TryComp<MiningConsoleComponent>(console, out var miningConsole) || miningConsole.LinkedServer == null
            || !TryComp<MiningAccountComponent>(miningConsole.LinkedServer, out var account))
            return;

        account.GlobalMode = mode;
        var query = EntityQueryEnumerator<MiningServerComponent>();
        while (query.MoveNext(out _, out var server))
            server.Mode = mode;
    }

    public void SetServerStage(EntityUid uid, int stage, MiningServerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.MiningStage = Math.Clamp(stage, 1, 3);
        if (TryComp<PowerConsumerComponent>(uid, out var consumer))
            consumer.DrawRate = comp.ActualPowerConsumption;

        UpdateAppearance(uid, comp);
    }

    public void SetGlobalActivation(EntityUid console, bool activate)
    {
        if (!TryComp<MiningConsoleComponent>(console, out var miningConsole) || miningConsole.LinkedServer == null
            || !TryComp<MiningAccountComponent>(miningConsole.LinkedServer, out var account))
            return;

        account.GlobalActivation = activate;
        var query = EntityQueryEnumerator<MiningServerComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var serverUid, out var server, out var consumer))
        {
            if (!server.IsBroken && consumer.ReceivedPower >= server.ActualPowerConsumption)
            {
                server.IsActive = activate;
                UpdateAppearance(serverUid, server);
                _ambient.SetAmbience(serverUid, activate);
            }
        }

        UpdateUi((console, miningConsole));
    }

    public void ToggleServerActivation(EntityUid serverUid)
    {
        if (!TryComp<MiningServerComponent>(serverUid, out var server) || server.IsBroken
            || !TryComp<PowerConsumerComponent>(serverUid, out var consumer) || consumer.ReceivedPower < server.ActualPowerConsumption)
            return;

        server.IsActive = !server.IsActive;
        UpdateAppearance(serverUid, server);
        _ambient.SetAmbience(serverUid, server.IsActive);
    }

    private void UpdateAppearance(EntityUid uid, MiningServerComponent? server = null)
    {
        if (!Resolve(uid, ref server))
            return;

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, MiningServerVisuals.MiningStage, server.MiningStage, appearance);
            _appearance.SetData(uid, MiningServerVisuals.IsActive, server.IsActive, appearance);
        }
    }
}
