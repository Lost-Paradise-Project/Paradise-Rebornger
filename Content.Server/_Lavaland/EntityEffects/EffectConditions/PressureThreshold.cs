using Content.Server._Lavaland.Procedural.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.EntityConditions;
using Content.Shared._Lavaland.EntityEffects.EffectConditions;

namespace Content.Server._Lavaland.EntityEffects.EffectConditions;

// Rewrited by BL02DL for updated SS14 version, now it works

public sealed class PressureThresholdConditionSystem : EntityConditionSystem<TransformComponent, PressureThresholdCondition>
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    protected override void Condition(Entity<TransformComponent> entity, ref EntityConditionEvent<PressureThresholdCondition> args)
    {
        if (args.Condition.WorksOnLavaland && HasComp<LavalandMapComponent>(entity.Comp.MapUid))
        {
            args.Result = true;
            return;
        }

        var mix = _atmos.GetTileMixture(entity.Owner);
        var pressure = mix?.Pressure ?? 0f;
        args.Result = pressure >= args.Condition.Min && pressure <= args.Condition.Max;
    }
}
