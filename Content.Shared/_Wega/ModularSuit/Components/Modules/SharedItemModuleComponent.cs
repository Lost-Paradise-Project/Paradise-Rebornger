using Robust.Shared.GameStates;

namespace Content.Shared._Wega.ModularSuit;

[NetworkedComponent]
public abstract partial class SharedItemModuleComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Module;
}
