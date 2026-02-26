using Content.Shared._White.RadialSelector;
using Robust.Shared.GameStates;

namespace Content.Shared._GoobStation.SetSelector;

[RegisterComponent, NetworkedComponent]
public sealed partial class RadialItemSelectorComponent : Component
{
    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = new();
}
