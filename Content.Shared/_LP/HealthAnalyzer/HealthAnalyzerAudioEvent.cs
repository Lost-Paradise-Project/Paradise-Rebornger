using Robust.Shared.Serialization;
using Content.Shared.Mobs;

namespace Content.Shared._LP.HealthAnalyzer;

[Serializable, NetSerializable]
public sealed class HealthAnalyzerAudioEvent : EntityEventArgs
{
    public MobState State;
    public bool ForceRestart;

    public HealthAnalyzerAudioEvent(
        MobState state,
        bool forceRestart = false)
    {
        State = state;
        ForceRestart = forceRestart;
    }
}

[Serializable, NetSerializable]
public sealed class HealthAnalyzerStopAudioEvent : EntityEventArgs
{
}
