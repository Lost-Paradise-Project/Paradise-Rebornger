using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._L5.Contract;

[RegisterComponent, NetworkedComponent]
public sealed partial class ContractComponent : Component
{
    [DataField]
    public ProtoId<ContractPrototype> Contract = default!;
}
