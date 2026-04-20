using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.ModularSuit;

[RegisterComponent]
public sealed partial class ModularSuitActionModuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Action;
}
