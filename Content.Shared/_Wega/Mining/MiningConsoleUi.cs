using Content.Shared._Wega.Mining.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Mining;

[Serializable, NetSerializable]
public enum MiningConsoleUiKey
{
    Key,
}

[Serializable, NetSerializable]
public enum MiningCircuitboardRepairUiKey // LP edit (add enum)
{
    Key,
}

[Serializable, NetSerializable]
public sealed class MiningConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public float Credits;
    public float ResearchPoints;
    public MiningMode Mode;
    public bool GlobalActivation;
    public List<MiningServerData> Servers;
    public int EnabledServersCount; // LP edit

    public MiningConsoleBoundInterfaceState(
        float credits,
        float researchPoints,
        MiningMode mode,
        bool globalActivation,
        List<MiningServerData> servers,
        int enabledServersCount) // LP edit
    {
        Credits = credits;
        ResearchPoints = researchPoints;
        Mode = mode;
        GlobalActivation = globalActivation;
        Servers = servers;
        EnabledServersCount = enabledServersCount; // LP edit
    }
}

[Serializable, NetSerializable]
public sealed class MiningConsoleToggleModeMessage : BoundUserInterfaceMessage
{
}

// LP edit start
[Serializable, NetSerializable]
public sealed class MiningConsoleActivateAllMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MiningConsoleDeactivateAllMessage : BoundUserInterfaceMessage
{
}
// LP edit end

[Serializable, NetSerializable]
public sealed class MiningConsoleToggleServerActivationMessage : BoundUserInterfaceMessage
{
    public NetEntity ServerUid;

    public MiningConsoleToggleServerActivationMessage(NetEntity serverUid)
    {
        ServerUid = serverUid;
    }
}

[Serializable, NetSerializable]
public sealed class MiningConsoleChangeServerStageMessage : BoundUserInterfaceMessage
{
    public NetEntity ServerUid;
    public int Delta;

    public MiningConsoleChangeServerStageMessage(NetEntity serverUid, int delta)
    {
        ServerUid = serverUid;
        Delta = delta;
    }
}

[Serializable, NetSerializable]
public sealed class MiningConsoleToggleUpdateMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MiningConsoleWithdrawMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed record MiningServerData(
    NetEntity Uid,
    int Stage,
    float Temperature,
    bool IsBroken,
    bool IsActive,
    float CircuitboardCondition
); // LP edit (add a float CircuitboardCondition)

[Serializable, NetSerializable]
public sealed class MiningConsoleSetAllStagesMessage : BoundUserInterfaceMessage
{
    public readonly int Stage;

    public MiningConsoleSetAllStagesMessage(int stage)
    {
        Stage = stage;
    }
}
