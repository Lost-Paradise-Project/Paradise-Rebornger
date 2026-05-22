using System.Linq;
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Lavaland.EntityShapes.Shapes;

/// <summary>
/// Represents a simple shape out of one horizontal and one vertical line
/// combined, similar to how Rook chess piece moves.
/// </summary>
public sealed partial class RookEntityShape : EntityShape
{
    protected override List<Vector2> GetShapeImplementation(IRobustRandom random, IPrototypeManager proto)
    {
        return ShapeHelpers.MakeCross(Offset, Size, StepSize).ToList();
    }
}
