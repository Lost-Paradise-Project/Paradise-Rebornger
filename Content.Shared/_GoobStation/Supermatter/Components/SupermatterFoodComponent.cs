using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared._GoobStation.Supermatter.Components;

[RegisterComponent]
public sealed partial class SupermatterFoodComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("energy")]
    public int Energy { get; set; } = 1;
}
