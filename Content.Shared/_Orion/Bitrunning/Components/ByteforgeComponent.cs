using Robust.Shared.GameStates;

namespace Content.Shared._Orion.Bitrunning.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ByteforgeComponent : Component
{
    public EntityUid? LinkedServer;

    public int VisualPulseSerial;
}
