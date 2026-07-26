using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityConditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._LP.EntityEffects.EntityConditions;

public sealed partial class TypedDamageThresholdSystem : EntityConditionSystem<DamageableComponent, TypedDamageThreshold>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    protected override void Condition(Entity<DamageableComponent> entity,
        ref EntityConditionEvent<TypedDamageThreshold> args)
    {
        var damage = entity.Comp.Damage;
        var spec = args.Condition.Damage;

        bool success = true;

        foreach (var (groupIdStr, required) in spec.DamageGroups)
        {
            if (required <= 0)
                continue;
#pragma warning disable CS0618
            if (!_proto.TryIndex<DamageGroupPrototype>(groupIdStr, out var group))
#pragma warning restore CS0618
            {
                success = false;
                break;
            }

            if (!damage.TryGetDamageInGroup(group, out var total) || total < required)
            {
                success = false;
                break;
            }
        }

        if (!success)
        {
            args.Result = args.Condition.Inverse ? true : false;
            return;
        }

        foreach (var (typeIdStr, required) in spec.DamageTypes)
        {
            if (required <= 0)
                continue;

            if (!_proto.TryIndex<DamageTypePrototype>(typeIdStr, out var type))
            {
                success = false;
                break;
            }

            if (!damage.DamageDict.TryGetValue(type.ID, out var current) || current < required)
            {
                success = false;
                break;
            }
        }

        args.Result = args.Condition.Inverse ? !success : success;
    }
}
