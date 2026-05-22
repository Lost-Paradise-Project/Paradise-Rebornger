using System.Linq;
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Lavaland.EntityShapes.Shapes;

/// <summary>
/// Represents a simple line with length of Size
/// made in some specified direction.
/// </summary>
public sealed partial class LineEntityShape : EntityShape
{
    [DataField]
    public Vector2 Direction = Vector2.UnitX;

    protected override List<Vector2> GetShapeImplementation(IRobustRandom random, IPrototypeManager proto)
    {
        return ShapeHelpers.MakeLine(Offset, Size, Direction).ToList();
    }
}
