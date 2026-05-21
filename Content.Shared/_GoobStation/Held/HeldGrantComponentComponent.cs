using Robust.Shared.Prototypes;

namespace Content.Shared._GoobStation.Held;

[RegisterComponent]
public sealed partial class HeldGrantComponentComponent : Component
{
    [DataField(required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<string, bool> Active = new();
}
