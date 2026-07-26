using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityConditions;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared._LP.EntityEffects.EntityConditions;

public sealed partial class TypedDamageThreshold : EntityConditionBase<TypedDamageThreshold>
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public bool Inverse;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        var damages = new List<string>();

        foreach (var (groupId, amount) in Damage.DamageGroups)
        {
#pragma warning disable CS0618
            if (!prototype.TryIndex(groupId, out DamageGroupPrototype? group))
#pragma warning restore CS0618
                continue;

            damages.Add(
                Loc.GetString(
                    "health-change-display",
                    ("kind", group.LocalizedName),
                    ("amount", MathF.Abs(amount.Float())),
                    ("deltasign", 1)));
        }

        foreach (var (typeId, amount) in Damage.DamageTypes)
        {
            if (!prototype.TryIndex(typeId, out DamageTypePrototype? type))
                continue;

            damages.Add(
                Loc.GetString(
                    "health-change-display",
                    ("kind", type.LocalizedName),
                    ("amount", MathF.Abs(amount.Float())),
                    ("deltasign", 1)));
        }

        return Loc.GetString(
            "entity-condition-guidebook-typed-damage-threshold",
            ("inverse", Inverse),
            ("changes", ContentLocalizationManager.FormatList(damages)));
    }
}
