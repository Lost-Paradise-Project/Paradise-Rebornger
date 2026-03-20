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
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public string Url = string.Empty;


    [DataField]
    public Vector2 scale = new(1, 1);
}
