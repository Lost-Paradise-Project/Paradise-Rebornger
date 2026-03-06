using Robust.Shared.Serialization;

namespace Content.Shared._LP.Mining.Components;

[RegisterComponent]
public sealed partial class MiningServerCircuitboardVisualsComponent : Component
{
}

[Serializable, NetSerializable]
public enum MiningServerCircuitboardVisuals : byte
{
    IsBroken
}
