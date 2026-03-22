using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared._LP.Reklama;

[Prototype("reklama")]
public sealed partial class ReklamaPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = string.Empty;

    [DataField(required: true)]
    public string Icon { get; private set; } = string.Empty;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public string Description { get; private set; } = string.Empty;

    [DataField]
    public string Url { get; private set; } = string.Empty;


    [DataField]
    public Vector2 scale { get; private set; } = new(1, 1);
}
