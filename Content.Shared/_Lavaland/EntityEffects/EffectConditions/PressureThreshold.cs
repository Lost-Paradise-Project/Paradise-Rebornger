using Robust.Shared.Prototypes;
using Content.Shared.EntityConditions;

namespace Content.Shared._Lavaland.EntityEffects.EffectConditions;

// Rewrited by BL02DL for updated SS14 version, now it works

public sealed partial class PressureThresholdCondition : EntityConditionBase<PressureThresholdCondition>
{
    [DataField]
    public bool WorksOnLavaland;

    [DataField]
    public float Min = float.MinValue;

    [DataField]
    public float Max = float.MaxValue;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString(
            "reagent-effect-condition-pressure-threshold",
            ("min", Min),
            ("max", Max));
    }
}
