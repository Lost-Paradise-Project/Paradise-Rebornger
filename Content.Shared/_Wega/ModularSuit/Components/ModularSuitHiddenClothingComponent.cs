using Robust.Shared.GameStates;

namespace Content.Shared._Wega.ModularSuit;

[Access(typeof(SharedModularSuitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ModularSuitHiddenClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityUid> HiddenItems = new();
}
