using System.Linq;
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Lavaland.EntityShapes.Shapes;

/// <summary>
/// Creates a filled box, but also with a chance of a tile to be missing, making it have random cavities.
/// </summary>
public sealed partial class BoxRandomEntityShape : EntityShape
{
    /// <summary>
    /// The chance for a tile to be filled in this random box.
    /// Always overrides RemoveAmount
    /// </summary>
    [DataField]
    public float? FilledChance;

    /// <summary>
    /// How many tiles we should exclude from a filled box.
    /// </summary>
    [DataField]
    public int? RemoveAmount;

    protected override List<Vector2> GetShapeImplementation(IRobustRandom random, IPrototypeManager proto)
    {
        if (FilledChance != null)
            return ShapeHelpers.MakeBoxChanceRandom(Offset, Size, random, FilledChance.Value, StepSize).ToList();

        if (RemoveAmount != null)
            return ShapeHelpers.MakeBoxCountRandom(Offset, Size, random, RemoveAmount.Value, StepSize).ToList();

        return ShapeHelpers.MakeBoxFilled(Offset, Size, StepSize).ToList();
    }
}
