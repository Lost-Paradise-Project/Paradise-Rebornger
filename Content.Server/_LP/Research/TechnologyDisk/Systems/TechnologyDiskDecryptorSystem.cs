using System.Linq;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Lathe;
using Content.Shared.Research.Components;
using Content.Shared._LP.Research.TechnologyDisk.Components;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Containers;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Split into one file per minigame (see the other TechnologyDiskDecryptorSystem.*.cs files) -
// this one is just the shared plumbing: slots, starting/finishing an attempt, pushing UI state.
public sealed partial class TechnologyDiskDecryptorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedLatheSystem _lathe = default!;
    [Dependency] private readonly ResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);
        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);

        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, DiskDecryptorStartManualMessage>(OnStartManual);
        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, DiskDecryptorStartAutoMessage>(OnStartAuto);
        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, DiskDecryptorLockCalibrationMessage>(OnLockCalibration);
        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, DiskDecryptorGridClickMessage>(OnGridClick);
        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, DiskDecryptorClaimMessage>(OnClaim);
        SubscribeLocalEvent<TechnologyDiskDecryptorComponent, DiskDecryptorSubmitCodeMessage>(OnSubmitCode);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<TechnologyDiskDecryptorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (uid, comp);

            // running out the clock is treated the same as a mistake for every minigame
            if (comp.Phase == DecryptorPhase.Manual && now >= comp.LayerDeadline)
                ApplyMistake(ent);
            else if (comp.Phase == DecryptorPhase.Manual && comp.MinigameKind == MinigameKind.SignalJam)
                TickSignalJam(ent, now);
            else if (comp.Phase == DecryptorPhase.AutoRunning)
            {
                UpdateAutoHint(ent, now);
                if (now >= comp.AutoDecryptFinishTime)
                    SucceedDecryption(ent);
            }
        }
    }

    private void OnEntityInserted(Entity<TechnologyDiskDecryptorComponent> ent, ref EntInsertedIntoContainerMessage args)
        => SetProcessorPresence(ent, args.Container.ID, true);

    private void OnEntityRemoved(Entity<TechnologyDiskDecryptorComponent> ent, ref EntRemovedFromContainerMessage args)
        => SetProcessorPresence(ent, args.Container.ID, false);

    private void SetProcessorPresence(Entity<TechnologyDiskDecryptorComponent> ent, string containerId, bool present)
    {
        if (containerId == ent.Comp.ProcessorSlotId)
            ent.Comp.HasProcessor = present;

        UpdateUi(ent);
    }

    private void OnBeforeUiOpen(Entity<TechnologyDiskDecryptorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUi(ent);
    }

    private bool TryGetDisk(Entity<TechnologyDiskDecryptorComponent> ent, out EntityUid disk, out EncryptedTechnologyDiskComponent? diskComp)
    {
        disk = default;
        diskComp = null;

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.DiskSlotId, out var slot) || slot.Item is not { } item)
            return false;

        disk = item;
        return TryComp(item, out diskComp);
    }

    // Shared validation for both start buttons: needs a phase of Idle, a disk with an
    // actual recipe rolled, and a linked R&D server to unlock into.
    private bool TryBeginAttempt(Entity<TechnologyDiskDecryptorComponent> ent, EntityUid actor, out EncryptedTechnologyDiskComponent? diskComp)
    {
        diskComp = null;

        if (ent.Comp.Phase != DecryptorPhase.Idle || !TryGetDisk(ent, out _, out diskComp) || diskComp == null || diskComp.Recipes is not { Count: > 0 })
            return false;

        if (!_research.TryGetClientServer(ent.Owner, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("disk-decryptor-no-server"), ent, actor);
            return false;
        }

        var tier = diskComp.Tier ?? 1;
        var kinds = ent.Comp.MinigameKindsPerTier.GetValueOrDefault(tier, new[] { MinigameKind.Calibration });

        ent.Comp.DiskTier = tier;
        ent.Comp.MinigameKind = _random.Pick(kinds);
        ent.Comp.TotalLayers = ent.Comp.LayersPerTier.GetValueOrDefault(tier, 1);
        ent.Comp.CurrentLayer = 0;
        ent.Comp.Integrity = 100;
        ent.Comp.PendingRecipes = diskComp.Recipes;
        ent.Comp.RevealedRecipeName = GetRecipeDisplayName(diskComp);
        ent.Comp.MaskedRecipeName = MaskString(ent.Comp.RevealedRecipeName, 0f);

        _itemSlots.SetLock(ent, ent.Comp.DiskSlotId, true);
        _itemSlots.SetLock(ent, ent.Comp.ProcessorSlotId, true);
        return true;
    }

    private void OnStartManual(Entity<TechnologyDiskDecryptorComponent> ent, ref DiskDecryptorStartManualMessage args)
    {
        if (TryBeginAttempt(ent, args.Actor, out _))
            StartLayer(ent);
    }

    private void OnStartAuto(Entity<TechnologyDiskDecryptorComponent> ent, ref DiskDecryptorStartAutoMessage args)
    {
        if (!ent.Comp.HasProcessor || !TryBeginAttempt(ent, args.Actor, out _))
            return;

        var duration = ent.Comp.AutoDecryptDurationPerTier.GetValueOrDefault(ent.Comp.DiskTier ?? 1, TimeSpan.FromSeconds(30));
        ent.Comp.AutoDecryptTotalDuration = duration;
        ent.Comp.NextHintUpdate = _timing.CurTime;
        ent.Comp.AutoDecryptFinishTime = _timing.CurTime + duration;
        ent.Comp.Phase = DecryptorPhase.AutoRunning;
        BeginTimer(ent, (float)duration.TotalSeconds);
    }

    private string GetRecipeDisplayName(EncryptedTechnologyDiskComponent diskComp)
    {
        if (diskComp.Recipes is not { Count: > 0 } || !_protoMan.TryIndex(diskComp.Recipes[0], out var recipe))
            return Loc.GetString("disk-decryptor-recipe-unknown");

        return _lathe.GetRecipeName(recipe);
    }

    private void BeginTimer(Entity<TechnologyDiskDecryptorComponent> ent, float seconds)
    {
        ent.Comp.SecondsTotal = seconds;
        ent.Comp.LayerDeadline = _timing.CurTime + TimeSpan.FromSeconds(seconds);
        UpdateUi(ent);
    }

    private void StartLayer(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        ent.Comp.Phase = DecryptorPhase.Manual;

        switch (ent.Comp.MinigameKind)
        {
            case MinigameKind.Calibration: StartCalibration(ent); break;
            case MinigameKind.Circuit: StartCircuit(ent); break;
            case MinigameKind.LightsOut: StartLightsOut(ent); break;
            case MinigameKind.MemoryPairs: StartMemoryPairs(ent); break;
            case MinigameKind.Breach: StartBreach(ent); break;
            case MinigameKind.Pipe: StartPipe(ent); break;
            case MinigameKind.Flow: StartFlow(ent); break;
            case MinigameKind.PatternRecall: StartPatternRecall(ent); break;
            case MinigameKind.CodeGuess: StartCodeGuess(ent); break;
            case MinigameKind.SignalJam: StartSignalJam(ent); break;
            case MinigameKind.FrequencyBands: StartFrequencyBands(ent); break;
            case MinigameKind.MineSweep: StartMineSweep(ent); break;
        }
    }

    private void CompleteLayer(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        ent.Comp.CurrentLayer++;
        ent.Comp.Integrity = Math.Min(100, ent.Comp.Integrity + 10);
        ent.Comp.MaskedRecipeName = MaskString(ent.Comp.RevealedRecipeName, (float)ent.Comp.CurrentLayer / ent.Comp.TotalLayers);

        if (ent.Comp.CurrentLayer >= ent.Comp.TotalLayers)
            SucceedDecryption(ent);
        else
            StartLayer(ent);
    }

    private void OnGridClick(Entity<TechnologyDiskDecryptorComponent> ent, ref DiskDecryptorGridClickMessage args)
    {
        if (ent.Comp.Phase != DecryptorPhase.Manual)
            return;

        switch (ent.Comp.MinigameKind)
        {
            case MinigameKind.Circuit: HandleCircuitClick(ent, args.Index); break;
            case MinigameKind.LightsOut: HandleLightsClick(ent, args.Index); break;
            case MinigameKind.MemoryPairs: HandleMemoryClick(ent, args.Index); break;
            case MinigameKind.Breach: HandleBreachClick(ent, args.Index); break;
            case MinigameKind.Pipe: HandlePipeClick(ent, args.Index); break;
            case MinigameKind.Flow: HandleFlowClick(ent, args.Index); break;
            case MinigameKind.PatternRecall: HandlePatternClick(ent, args.Index); break;
            case MinigameKind.CodeGuess: HandleCodeGuessClick(ent, args.Index); break;
            case MinigameKind.SignalJam: HandleSignalJamClick(ent, args.Index); break;
            case MinigameKind.FrequencyBands: HandleFrequencyBandsClick(ent, args.Index); break;
            case MinigameKind.MineSweep: HandleMineSweepClick(ent, args.Index); break;
        }
    }

    private void ApplyMistake(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        ent.Comp.Integrity = Math.Max(0, ent.Comp.Integrity - ent.Comp.IntegrityPenaltyPerMistake);
        _audio.PlayPvs(ent.Comp.MistakeSound, ent);

        if (ent.Comp.Integrity <= 0)
        {
            FailDecryption(ent);
            return;
        }

        switch (ent.Comp.MinigameKind)
        {
            case MinigameKind.Calibration:
                StartCalibration(ent);
                ent.Comp.LayerDeadline -= ent.Comp.TimePenaltyPerMistake;
                break;
            case MinigameKind.Circuit:
                ent.Comp.CircuitPath = new() { ent.Comp.CircuitStartIndex }; // same maze, walk back to the start
                ent.Comp.CircuitMovesLeft = ent.Comp.CircuitMovesBudget; // otherwise repeated mistakes leave too few moves to ever finish
                ent.Comp.LayerDeadline -= ent.Comp.TimePenaltyPerMistake;
                break;
            case MinigameKind.Breach:
                ent.Comp.BreachPath = new(); // same board, clear the picks
                ent.Comp.BreachMovesLeft = ent.Comp.BreachMovesBudget;
                ent.Comp.LayerDeadline -= ent.Comp.TimePenaltyPerMistake;
                break;
            case MinigameKind.Flow:
                ent.Comp.FlowPath = new() { ent.Comp.FlowStartIndex }; // same network, walk back to the start
                ent.Comp.FlowNextCheckpoint = 0;
                ent.Comp.FlowMovesLeft = ent.Comp.FlowMovesBudget;
                ent.Comp.LayerDeadline -= ent.Comp.TimePenaltyPerMistake;
                break;
            case MinigameKind.SignalJam:
                SpawnJamTarget(ent); // just the current target respawns, JamHitsDone is untouched
                break;
            default:
                StartLayer(ent); // LightsOut/MemoryPairs/Pipe/PatternRecall/CodeGuess/FrequencyBands/MineSweep just regenerate a fresh puzzle
                break;
        }

        UpdateUi(ent);
    }

    private void UpdateAutoHint(Entity<TechnologyDiskDecryptorComponent> ent, TimeSpan now)
    {
        if (now < ent.Comp.NextHintUpdate)
            return;

        ent.Comp.NextHintUpdate = now + TimeSpan.FromSeconds(1);

        var total = ent.Comp.AutoDecryptTotalDuration.TotalSeconds;
        var remaining = (ent.Comp.AutoDecryptFinishTime - now).TotalSeconds;
        var elapsedFraction = total <= 0 ? 1f : 1f - (float)(remaining / total);

        ent.Comp.MaskedRecipeName = MaskString(ent.Comp.RevealedRecipeName, elapsedFraction);
        UpdateUi(ent);
    }

    private void OnClaim(Entity<TechnologyDiskDecryptorComponent> ent, ref DiskDecryptorClaimMessage args)
    {
        if (ent.Comp.Phase == DecryptorPhase.Idle)
            return;

        GrantRefusalPoints(ent);
        _popup.PopupEntity(Loc.GetString("disk-decryptor-claimed"), ent, args.Actor);
        Log.Info($"{ToPrettyString(args.Actor)} cancelled decryption at {ToPrettyString(ent.Owner)}, took points instead");
        EjectAndDeleteDisk(ent);
        ResetDevice(ent);
    }

    private void SucceedDecryption(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        if (ent.Comp.PendingRecipes != null &&
            _research.TryGetClientServer(ent.Owner, out var server, out _) &&
            TryComp(server, out TechnologyDatabaseComponent? database))
        {
            foreach (var recipe in ent.Comp.PendingRecipes)
            {
                _research.AddLatheRecipe(server.Value, recipe, database);
            }
        }

        _audio.PlayPvs(ent.Comp.SuccessSound, ent);
        _popup.PopupEntity(Loc.GetString("disk-decryptor-success", ("recipe", ent.Comp.RevealedRecipeName)), ent);
        Log.Info($"Decryption succeeded at {ToPrettyString(ent.Owner)}, tier {ent.Comp.DiskTier}");
        EjectAndDeleteDisk(ent);
        ResetDevice(ent);
    }

    private void FailDecryption(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        _audio.PlayPvs(ent.Comp.FailSound, ent);
        _popup.PopupEntity(Loc.GetString("disk-decryptor-fail"), ent);
        Log.Info($"Decryption failed at {ToPrettyString(ent.Owner)}, tier {ent.Comp.DiskTier} disk destroyed");
        EjectAndDeleteDisk(ent);
        ResetDevice(ent);
    }

    private void GrantRefusalPoints(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        if (ent.Comp.DiskTier is not { } tier || !ent.Comp.RefusalPointsRangePerTier.TryGetValue(tier, out var range))
            return;

        if (_research.TryGetClientServer(ent.Owner, out var server, out var serverComp))
            _research.ModifyServerPoints(server.Value, _random.Next(range.X, range.Y + 1), serverComp);
    }

    private void EjectAndDeleteDisk(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        if (_itemSlots.TryGetSlot(ent, ent.Comp.DiskSlotId, out var slot) && slot.Item is { } disk)
            QueueDel(disk);
    }

    private void ResetDevice(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var comp = ent.Comp;
        comp.Phase = DecryptorPhase.Idle;
        comp.DiskTier = null;
        comp.CurrentLayer = 0;
        comp.TotalLayers = 0;
        comp.Integrity = 100;
        comp.RevealedRecipeName = string.Empty;
        comp.MaskedRecipeName = string.Empty;
        comp.PendingRecipes = null;
        comp.SecondsTotal = 0;

        comp.CircuitBlocked = new();
        comp.CircuitPath = new();
        comp.CircuitMovesLeft = 0;
        comp.CircuitMovesBudget = 0;
        comp.LightsState = new();
        comp.MemorySymbols = new();
        comp.MemoryMatched = new();
        comp.MemorySelected.Clear();
        comp.BreachCodes = new();
        comp.BreachSequence = new();
        comp.BreachPath = new();
        comp.BreachMovesLeft = 0;
        comp.BreachMovesBudget = 0;
        comp.PipeBaseMask = new();
        comp.PipeRotation = new();
        comp.FlowOpen = new();
        comp.FlowCheckpoints = new();
        comp.FlowPath = new();
        comp.FlowNextCheckpoint = 0;
        comp.FlowMovesLeft = 0;
        comp.FlowMovesBudget = 0;
        comp.PatternTargets = new();
        comp.PatternFound = new();
        comp.CodeSecret = new();
        comp.CodeGuess = new();
        comp.CodeAttemptsLeft = 0;
        comp.CodeAttemptsBudget = 0;
        comp.CodeLastExact = -1;
        comp.CodeLastPartial = -1;
        comp.JamActiveCell = -1;
        comp.JamHitsDone = 0;
        comp.JamHitsNeeded = 0;
        comp.BandsPeriod = new();
        comp.BandsZoneStart = new();
        comp.BandsZoneWidth = new();
        comp.BandsStartTime = new();
        comp.BandsSolved = new();
        comp.SweepMines = new();
        comp.SweepRevealed = new();
        comp.SweepFirstClickDone = false;

        _itemSlots.SetLock(ent, comp.DiskSlotId, false);
        _itemSlots.SetLock(ent, comp.ProcessorSlotId, false);
        UpdateUi(ent);
    }

    private static string MaskString(string input, float revealFraction)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var revealCount = (int)Math.Ceiling(input.Length * Math.Clamp(revealFraction, 0f, 1f));
        var chars = input.ToCharArray();
        for (var i = revealCount; i < chars.Length; i++)
        {
            if (chars[i] != ' ')
                chars[i] = '█';
        }

        return new string(chars);
    }

    private void UpdateUi(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var comp = ent.Comp;
        var deadline = comp.Phase switch
        {
            DecryptorPhase.Manual => comp.LayerDeadline,
            DecryptorPhase.AutoRunning => comp.AutoDecryptFinishTime,
            _ => TimeSpan.Zero,
        };

        // only send a minigame's board state while it's the active one - keeps stale/secret data off the wire
        T? ForKind<T>(MinigameKind kind, T value) where T : class => comp.MinigameKind == kind ? value : null;

        // don't leak the memory-pairs answers - only flipped/matched cells get a real value
        var visibleSymbols = comp.MinigameKind != MinigameKind.MemoryPairs
            ? null
            : comp.MemorySymbols.Select((symbol, i) => comp.MemoryMatched[i] || comp.MemorySelected.Contains(i) ? symbol : -1).ToList();

        // don't leak mine locations - only revealed cells get a real number
        var sweepVisible = comp.MinigameKind != MinigameKind.MineSweep
            ? null
            : Enumerable.Range(0, comp.SweepMines.Count)
                .Select(i => comp.SweepRevealed[i] ? CountAdjacentMines(comp.SweepMines, i, comp.SweepSize) : -1)
                .ToList();

        var state = new DiskDecryptorBoundUserInterfaceState(
            TryGetDisk(ent, out _, out _),
            comp.HasProcessor,
            comp.Phase,
            comp.MinigameKind,
            comp.DiskTier,
            comp.CurrentLayer,
            comp.TotalLayers,
            comp.Integrity,
            comp.MaskedRecipeName,
            deadline,
            comp.SecondsTotal,
            comp.CalibrationStartTime,
            comp.CalibrationPeriodSeconds,
            comp.CalibrationZoneStart,
            comp.CalibrationZoneWidth,
            comp.CircuitSize,
            ForKind(MinigameKind.Circuit, comp.CircuitBlocked),
            comp.CircuitStartIndex,
            comp.CircuitEndIndex,
            ForKind(MinigameKind.Circuit, comp.CircuitPath),
            comp.CircuitMovesLeft,
            comp.LightsSize,
            ForKind(MinigameKind.LightsOut, comp.LightsState),
            visibleSymbols,
            ForKind(MinigameKind.MemoryPairs, comp.MemoryMatched),
            comp.BreachSize,
            ForKind(MinigameKind.Breach, comp.BreachCodes),
            ForKind(MinigameKind.Breach, comp.BreachSequence),
            ForKind(MinigameKind.Breach, comp.BreachPath),
            comp.BreachMovesLeft,
            comp.PipeSize,
            ForKind(MinigameKind.Pipe, comp.PipeBaseMask),
            ForKind(MinigameKind.Pipe, comp.PipeRotation),
            comp.PipeStartIndex,
            comp.PipeEndIndex,
            comp.FlowSize,
            ForKind(MinigameKind.Flow, comp.FlowOpen),
            comp.FlowStartIndex,
            ForKind(MinigameKind.Flow, comp.FlowCheckpoints),
            comp.FlowNextCheckpoint,
            ForKind(MinigameKind.Flow, comp.FlowPath),
            comp.FlowMovesLeft,
            comp.PatternSize,
            ForKind(MinigameKind.PatternRecall, comp.PatternTargets),
            ForKind(MinigameKind.PatternRecall, comp.PatternFound),
            comp.PatternRevealEndTime,
            comp.PatternRevealSeconds,
            comp.CodeLength,
            comp.CodeColorCount,
            ForKind(MinigameKind.CodeGuess, comp.CodeGuess),
            comp.CodeAttemptsLeft,
            comp.CodeAttemptsBudget,
            comp.CodeLastExact,
            comp.CodeLastPartial,
            comp.JamSize,
            comp.JamActiveCell,
            comp.JamActiveDeadline,
            comp.JamHitsNeeded,
            comp.JamHitsDone,
            ForKind(MinigameKind.FrequencyBands, comp.BandsPeriod),
            ForKind(MinigameKind.FrequencyBands, comp.BandsZoneStart),
            ForKind(MinigameKind.FrequencyBands, comp.BandsZoneWidth),
            ForKind(MinigameKind.FrequencyBands, comp.BandsStartTime),
            ForKind(MinigameKind.FrequencyBands, comp.BandsSolved),
            comp.SweepSize,
            sweepVisible);

        _ui.SetUiState(ent.Owner, TechnologyDiskDecryptorUiKey.Key, state);
    }
}
