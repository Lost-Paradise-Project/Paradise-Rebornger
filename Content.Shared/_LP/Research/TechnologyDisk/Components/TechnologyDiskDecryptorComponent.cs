using Content.Shared.Research.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._LP.Research.TechnologyDisk.Components;

// All of this is just server-side bookkeeping for whatever decryption attempt is
// currently running. The UI never reads this component directly - everything the
// client needs goes out through DiskDecryptorBoundUserInterfaceState instead - so
// there's nothing here that needs to be networked.
[RegisterComponent]
public sealed partial class TechnologyDiskDecryptorComponent : Component
{
    [DataField]
    public string DiskSlotId = "disk_slot";

    [DataField]
    public string ProcessorSlotId = "processor_slot";

    public DecryptorPhase Phase = DecryptorPhase.Idle;
    public bool HasProcessor;
    public int? DiskTier;
    public MinigameKind MinigameKind;
    public int CurrentLayer;
    public int TotalLayers;
    public int Integrity = 100;
    public TimeSpan LayerDeadline;
    public TimeSpan AutoDecryptFinishTime;
    public string RevealedRecipeName = string.Empty;
    public string MaskedRecipeName = string.Empty;
    public float SecondsTotal;

    // Calibration: a marker bounces back and forth along [0,1], player has to lock it in while it's inside the target zone.
    public TimeSpan CalibrationStartTime;
    public float CalibrationPeriodSeconds;
    public float CalibrationZoneStart;
    public float CalibrationZoneWidth;

    // Memory pairs: classic card-matching. MemorySymbols is the real board and is
    // never sent to the client - UpdateUi builds a "what the player can currently see" copy of it instead.
    public List<int> MemorySymbols = new();
    public List<bool> MemoryMatched = new();
    public List<int> MemorySelected = new();

    // Pipe: PipeBaseMask[i] is a segment's *correct* connection bitmask (0 = not a pipe
    // cell at all), PipeRotation[i] is how many quarter-turns it's currently off from that.
    public int PipeSize;
    public List<int> PipeBaseMask = new();
    public List<int> PipeRotation = new();
    public int PipeStartIndex;
    public int PipeEndIndex;

    public int PatternSize;
    public List<bool> PatternTargets = new();
    public List<bool> PatternFound = new();
    public TimeSpan PatternRevealEndTime;
    public float PatternRevealSeconds;

    public int LightsSize;
    public List<bool> LightsState = new();

    public int CodeLength;
    public int CodeColorCount;
    public List<int> CodeSecret = new();
    public List<int> CodeGuess = new();
    public int CodeAttemptsLeft;
    public int CodeAttemptsBudget;
    public int CodeLastExact = -1;
    public int CodeLastPartial = -1;

    public int JamSize;
    public int JamActiveCell = -1;
    public TimeSpan JamActiveDeadline;
    public int JamHitsNeeded;
    public int JamHitsDone;

    public List<float> BandsPeriod = new();
    public List<float> BandsZoneStart = new();
    public List<float> BandsZoneWidth = new();
    public List<TimeSpan> BandsStartTime = new();
    public List<bool> BandsSolved = new();

    public int CircuitSize;
    public List<bool> CircuitBlocked = new();
    public int CircuitStartIndex;
    public int CircuitEndIndex;
    public List<int> CircuitPath = new();
    public int CircuitMovesLeft;
    public int CircuitMovesBudget; // restored on every retry so a mistake doesn't slowly starve the player of moves

    public int BreachSize;
    public List<string> BreachCodes = new();
    public List<string> BreachSequence = new();
    public List<int> BreachPath = new();
    public int BreachMovesLeft;
    public int BreachMovesBudget;

    public int FlowSize;
    public List<bool> FlowOpen = new();
    public int FlowStartIndex;
    public List<int> FlowCheckpoints = new(); // ordered node indices to hit - the last one is the final target
    public int FlowNextCheckpoint;
    public List<int> FlowPath = new();
    public int FlowMovesLeft;
    public int FlowMovesBudget;

    public int SweepSize;
    public List<bool> SweepMines = new();
    public List<bool> SweepRevealed = new();
    public bool SweepFirstClickDone;

    public TimeSpan AutoDecryptTotalDuration;
    public TimeSpan NextHintUpdate;

    [DataField]
    public int IntegrityPenaltyPerMistake = 20;

    [DataField]
    public TimeSpan TimePenaltyPerMistake = TimeSpan.FromSeconds(3);

    [DataField]
    public Dictionary<int, int> LayersPerTier = new()
    {
        [1] = 1,
        [2] = 2,
        [3] = 3,
    };

    // Which minigames can show up for a given tier - one is picked at random each time
    // so the same disk doesn't feel like the same chore every run.
    [DataField]
    public Dictionary<int, MinigameKind[]> MinigameKindsPerTier = new()
    {
        [1] = new[] { MinigameKind.Calibration, MinigameKind.MemoryPairs, MinigameKind.Pipe, MinigameKind.PatternRecall },
        [2] = new[] { MinigameKind.LightsOut, MinigameKind.CodeGuess, MinigameKind.SignalJam, MinigameKind.FrequencyBands },
        [3] = new[] { MinigameKind.Circuit, MinigameKind.Breach, MinigameKind.Flow, MinigameKind.MineSweep },
    };

    [DataField]
    public Dictionary<int, TimeSpan> AutoDecryptDurationPerTier = new()
    {
        [1] = TimeSpan.FromSeconds(15),
        [2] = TimeSpan.FromSeconds(35),
        [3] = TimeSpan.FromSeconds(70),
    };

    [DataField]
    public Dictionary<int, Vector2i> RefusalPointsRangePerTier = new()
    {
        [1] = new Vector2i(1000, 1800),
        [2] = new Vector2i(2000, 3500),
        [3] = new Vector2i(4000, 7000),
    };

    [DataField]
    public SoundSpecifier SuccessSound = new SoundPathSpecifier("/Audio/_LP/Structures/Machines/decryp_success.ogg");

    [DataField]
    public SoundSpecifier FailSound = new SoundPathSpecifier("/Audio/_LP/Structures/Machines/decryp_fail.ogg");

    [DataField]
    public SoundSpecifier MistakeSound = new SoundPathSpecifier("/Audio/_LP/Structures/Machines/decryp_mistake.ogg");

    [DataField]
    public SoundSpecifier NodeSound = new SoundPathSpecifier("/Audio/_LP/Structures/Machines/decryp_node_click.ogg");

    /// <summary>
    /// Recipes pending unlock on success, copied off the disk when the attempt starts
    /// so the disk itself can be safely deleted at any point afterwards.
    /// </summary>
    public List<ProtoId<LatheRecipePrototype>>? PendingRecipes;
}

public enum DecryptorPhase : byte
{
    Idle,
    Manual,
    AutoRunning,
}

public enum MinigameKind : byte
{
    Calibration,
    Circuit,
    LightsOut,
    MemoryPairs,
    Breach,
    Pipe,
    Flow,
    PatternRecall,
    CodeGuess,
    SignalJam,
    FrequencyBands,
    MineSweep,
}

[Serializable, NetSerializable]
public enum TechnologyDiskDecryptorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DiskDecryptorStartManualMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DiskDecryptorStartAutoMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DiskDecryptorLockCalibrationMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// A click on the puzzle grid - used by Circuit, Lights Out and Memory Pairs alike.
/// What it actually does depends on which of those is currently active.
/// </summary>
[Serializable, NetSerializable]
public sealed class DiskDecryptorGridClickMessage : BoundUserInterfaceMessage
{
    public readonly int Index;

    public DiskDecryptorGridClickMessage(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class DiskDecryptorClaimMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DiskDecryptorSubmitCodeMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DiskDecryptorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool HasDisk;
    public readonly bool HasProcessor;
    public readonly DecryptorPhase Phase;
    public readonly MinigameKind MinigameKind;
    public readonly int? DiskTier;
    public readonly int CurrentLayer;
    public readonly int TotalLayers;
    public readonly int Integrity;
    public readonly string MaskedRecipeName;

    // See TechnologyDiskDecryptorWindow.FrameUpdate - the client uses this plus its own
    // clock to animate the time bar and calibration marker smoothly instead of waiting
    // on the server to push an update every frame.
    public readonly TimeSpan Deadline;
    public readonly float SecondsTotal;

    public readonly TimeSpan CalibrationStartTime;
    public readonly float CalibrationPeriodSeconds;
    public readonly float CalibrationZoneStart;
    public readonly float CalibrationZoneWidth;

    public readonly int CircuitSize;
    public readonly List<bool>? CircuitBlocked;
    public readonly int CircuitStartIndex;
    public readonly int CircuitEndIndex;
    public readonly List<int>? CircuitPath;
    public readonly int CircuitMovesLeft;

    public readonly int LightsSize;
    public readonly List<bool>? LightsState;
    public readonly List<int>? MemoryVisibleSymbols;
    public readonly List<bool>? MemoryMatched;

    public readonly int BreachSize;
    public readonly List<string>? BreachCodes;
    public readonly List<string>? BreachSequence;
    public readonly List<int>? BreachPath;
    public readonly int BreachMovesLeft;

    public readonly int PipeSize;
    public readonly List<int>? PipeBaseMask;
    public readonly List<int>? PipeRotation;
    public readonly int PipeStartIndex;
    public readonly int PipeEndIndex;

    public readonly int FlowSize;
    public readonly List<bool>? FlowOpen;
    public readonly int FlowStartIndex;
    public readonly List<int>? FlowCheckpoints;
    public readonly int FlowNextCheckpoint;
    public readonly List<int>? FlowPath;
    public readonly int FlowMovesLeft;

    public readonly int PatternSize;
    public readonly List<bool>? PatternTargets;
    public readonly List<bool>? PatternFound;
    public readonly TimeSpan PatternRevealEndTime;
    public readonly float PatternRevealSeconds;

    /// <summary>The secret code is never sent - only the guess-in-progress and last feedback.</summary>
    public readonly int CodeLength;
    public readonly int CodeColorCount;
    public readonly List<int>? CodeGuess;
    public readonly int CodeAttemptsLeft;
    public readonly int CodeAttemptsBudget;
    public readonly int CodeLastExact;
    public readonly int CodeLastPartial;

    public readonly int JamSize;
    public readonly int JamActiveCell;
    public readonly TimeSpan JamActiveDeadline;
    public readonly int JamHitsNeeded;
    public readonly int JamHitsDone;

    public readonly List<float>? BandsPeriod;
    public readonly List<float>? BandsZoneStart;
    public readonly List<float>? BandsZoneWidth;
    public readonly List<TimeSpan>? BandsStartTime;
    public readonly List<bool>? BandsSolved;

    /// <summary>-1 = still hidden, 0-8 = revealed safe cell's adjacent trap count. Trap locations are never sent for unrevealed cells.</summary>
    public readonly int SweepSize;
    public readonly List<int>? SweepVisible;

    public DiskDecryptorBoundUserInterfaceState(
        bool hasDisk,
        bool hasProcessor,
        DecryptorPhase phase,
        MinigameKind minigameKind,
        int? diskTier,
        int currentLayer,
        int totalLayers,
        int integrity,
        string maskedRecipeName,
        TimeSpan deadline,
        float secondsTotal,
        TimeSpan calibrationStartTime,
        float calibrationPeriodSeconds,
        float calibrationZoneStart,
        float calibrationZoneWidth,
        int circuitSize,
        List<bool>? circuitBlocked,
        int circuitStartIndex,
        int circuitEndIndex,
        List<int>? circuitPath,
        int circuitMovesLeft,
        int lightsSize,
        List<bool>? lightsState,
        List<int>? memoryVisibleSymbols,
        List<bool>? memoryMatched,
        int breachSize,
        List<string>? breachCodes,
        List<string>? breachSequence,
        List<int>? breachPath,
        int breachMovesLeft,
        int pipeSize,
        List<int>? pipeBaseMask,
        List<int>? pipeRotation,
        int pipeStartIndex,
        int pipeEndIndex,
        int flowSize,
        List<bool>? flowOpen,
        int flowStartIndex,
        List<int>? flowCheckpoints,
        int flowNextCheckpoint,
        List<int>? flowPath,
        int flowMovesLeft,
        int patternSize,
        List<bool>? patternTargets,
        List<bool>? patternFound,
        TimeSpan patternRevealEndTime,
        float patternRevealSeconds,
        int codeLength,
        int codeColorCount,
        List<int>? codeGuess,
        int codeAttemptsLeft,
        int codeAttemptsBudget,
        int codeLastExact,
        int codeLastPartial,
        int jamSize,
        int jamActiveCell,
        TimeSpan jamActiveDeadline,
        int jamHitsNeeded,
        int jamHitsDone,
        List<float>? bandsPeriod,
        List<float>? bandsZoneStart,
        List<float>? bandsZoneWidth,
        List<TimeSpan>? bandsStartTime,
        List<bool>? bandsSolved,
        int sweepSize,
        List<int>? sweepVisible)
    {
        HasDisk = hasDisk;
        HasProcessor = hasProcessor;
        Phase = phase;
        MinigameKind = minigameKind;
        DiskTier = diskTier;
        CurrentLayer = currentLayer;
        TotalLayers = totalLayers;
        Integrity = integrity;
        MaskedRecipeName = maskedRecipeName;
        Deadline = deadline;
        SecondsTotal = secondsTotal;
        CalibrationStartTime = calibrationStartTime;
        CalibrationPeriodSeconds = calibrationPeriodSeconds;
        CalibrationZoneStart = calibrationZoneStart;
        CalibrationZoneWidth = calibrationZoneWidth;
        CircuitSize = circuitSize;
        CircuitBlocked = circuitBlocked;
        CircuitStartIndex = circuitStartIndex;
        CircuitEndIndex = circuitEndIndex;
        CircuitPath = circuitPath;
        CircuitMovesLeft = circuitMovesLeft;
        LightsSize = lightsSize;
        LightsState = lightsState;
        MemoryVisibleSymbols = memoryVisibleSymbols;
        MemoryMatched = memoryMatched;
        BreachSize = breachSize;
        BreachCodes = breachCodes;
        BreachSequence = breachSequence;
        BreachPath = breachPath;
        BreachMovesLeft = breachMovesLeft;
        PipeSize = pipeSize;
        PipeBaseMask = pipeBaseMask;
        PipeRotation = pipeRotation;
        PipeStartIndex = pipeStartIndex;
        PipeEndIndex = pipeEndIndex;
        FlowSize = flowSize;
        FlowOpen = flowOpen;
        FlowStartIndex = flowStartIndex;
        FlowCheckpoints = flowCheckpoints;
        FlowNextCheckpoint = flowNextCheckpoint;
        FlowPath = flowPath;
        FlowMovesLeft = flowMovesLeft;
        PatternSize = patternSize;
        PatternTargets = patternTargets;
        PatternFound = patternFound;
        PatternRevealEndTime = patternRevealEndTime;
        PatternRevealSeconds = patternRevealSeconds;
        CodeLength = codeLength;
        CodeColorCount = codeColorCount;
        CodeGuess = codeGuess;
        CodeAttemptsLeft = codeAttemptsLeft;
        CodeAttemptsBudget = codeAttemptsBudget;
        CodeLastExact = codeLastExact;
        CodeLastPartial = codeLastPartial;
        JamSize = jamSize;
        JamActiveCell = jamActiveCell;
        JamActiveDeadline = jamActiveDeadline;
        JamHitsNeeded = jamHitsNeeded;
        JamHitsDone = jamHitsDone;
        BandsPeriod = bandsPeriod;
        BandsZoneStart = bandsZoneStart;
        BandsZoneWidth = bandsZoneWidth;
        BandsStartTime = bandsStartTime;
        BandsSolved = bandsSolved;
        SweepSize = sweepSize;
        SweepVisible = sweepVisible;
    }
}
