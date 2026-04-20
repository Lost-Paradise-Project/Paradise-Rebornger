namespace Content.Shared._Wega.ModularSuit;

[RegisterComponent]
public sealed partial class ModularSuitSpringlockModuleComponent : Component;

[RegisterComponent]
public sealed partial class ModularSuitSpringlockInstalledComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Module = default!;
}
