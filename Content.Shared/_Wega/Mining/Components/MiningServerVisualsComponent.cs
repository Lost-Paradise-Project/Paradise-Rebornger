using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Mining.Components;

[RegisterComponent]
public sealed partial class MiningServerVisualsComponent : Component
{
}

[Serializable, NetSerializable]
public enum MiningServerVisuals : byte
{
    MiningStage,
    IsActive
}
