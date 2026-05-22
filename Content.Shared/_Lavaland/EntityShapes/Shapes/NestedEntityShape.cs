using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Lavaland.EntityShapes.Shapes;

/// <summary>
/// Shape that references a ProtoId containing some other shape.
/// </summary>
public sealed partial class NestedEntityShape : EntityShape
{
    [DataField(required: true)]
    public ProtoId<EntityShapePrototype> Id;

    protected override List<Vector2> GetShapeImplementation(IRobustRandom random, IPrototypeManager proto)
    {
        return proto.Index(Id).Shape.GetShape(random, proto, Offset, Size, StepSize);
    }
}
