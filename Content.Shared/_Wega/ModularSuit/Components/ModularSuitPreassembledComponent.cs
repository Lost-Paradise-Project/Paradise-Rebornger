using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.ModularSuit;

[RegisterComponent]
public sealed partial class ModularSuitPreassembledComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Modules = new();
}
