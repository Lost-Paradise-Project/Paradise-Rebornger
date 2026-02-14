using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._L5.Contract;

[Prototype]
public sealed partial class ContractPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public LocId Name { get; private set; } = "contract-unknown-name";

    [DataField]
    public LocId Description { get; private set; } = "contract-unknown-desc";

    [DataField]
    public bool Selectable  { get; private set; } = true;
}
