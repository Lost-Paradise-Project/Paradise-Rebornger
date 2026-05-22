using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Lavaland.EntityShapes.Shapes;

/// <summary>
/// Returns a singe tile at the specified position.
/// </summary>
public sealed partial class SingleEntityShape : EntityShape
{
    protected override List<Vector2> GetShapeImplementation(IRobustRandom random, IPrototypeManager proto)
    {
        return new List<Vector2> { Offset };
    }
}
