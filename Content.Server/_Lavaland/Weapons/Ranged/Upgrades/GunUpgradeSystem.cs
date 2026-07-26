using Content.Server._Lavaland.Pressure;
using Content.Server._Lavaland.Weapons.Ranged.Upgrades.Components;
using Content.Shared._Lavaland.Weapons.Ranged.Events;
using Content.Shared._Lavaland.Weapons.Ranged.Upgrades;
using Content.Shared._Lavaland.Weapons.Ranged.Upgrades.Components;
using Content.Shared._GoobStation.EntityEffects;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Lavaland.Weapons.Ranged.Upgrades;

public sealed class GunUpgradeSystem : SharedGunUpgradeSystem
{
    [Dependency] private readonly PressureEfficiencyChangeSystem _pressure = default!;
    [Dependency] private readonly SharedEntityEffectSystem _entityEffect = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunUpgradeDamageComponent, GunShotEvent>(OnDamageGunShot);
        SubscribeLocalEvent<GunUpgradeDamageComponent, ProjectileShotEvent>(OnProjectileShot);
        SubscribeLocalEvent<GunUpgradePressureComponent, EntGotInsertedIntoContainerMessage>(OnPressureUpgradeInserted);
        SubscribeLocalEvent<GunUpgradePressureComponent, EntGotRemovedFromContainerMessage>(OnPressureUpgradeRemoved);

        SubscribeLocalEvent<WeaponUpgradeEffectsComponent, MeleeHitEvent>(OnEffectsUpgradeHit);
    }

    private void OnDamageGunShot(Entity<GunUpgradeDamageComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (!TryComp<ProjectileComponent>(ammo, out var projectile))
                continue;

            var multiplier = 1f;

            if (TryComp<PressureDamageChangeComponent>(Transform(ent).ParentUid, out var pressure)
                && _pressure.ApplyModifier((Transform(ent).ParentUid, pressure))
                && pressure.ApplyToProjectiles)
                multiplier = pressure.AppliedModifier;

            if (ent.Comp.BonusDamage != null)
                projectile.Damage += ent.Comp.BonusDamage * multiplier;
            projectile.Damage *= ent.Comp.Modifier;
        }
    }

    private void OnProjectileShot(Entity<GunUpgradeDamageComponent> ent, ref ProjectileShotEvent args)
    {
        if (!TryComp<ProjectileComponent>(args.FiredProjectile, out var projectile))
            return;

        var multiplier = 1f;

        if (TryComp<PressureDamageChangeComponent>(Transform(ent).ParentUid, out var pressure)
            && _pressure.ApplyModifier((Transform(ent).ParentUid, pressure))
            && pressure.ApplyToProjectiles)
            multiplier = pressure.AppliedModifier;

        if (ent.Comp.BonusDamage != null)
            projectile.Damage += ent.Comp.BonusDamage * multiplier;
        projectile.Damage *= ent.Comp.Modifier;
    }

    private void OnPressureUpgradeInserted(Entity<GunUpgradePressureComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        var comp = ent.Comp;
        if (!TryComp<PressureDamageChangeComponent>(args.Container.Owner, out var pdc))
            return;

        comp.SavedAppliedModifier = pdc.AppliedModifier;
        comp.SavedApplyWhenInRange = pdc.ApplyWhenInRange;
        comp.SavedLowerBound = pdc.LowerBound;
        comp.SavedUpperBound = pdc.UpperBound;

        if (comp.NewAppliedModifier != null)
            pdc.AppliedModifier = comp.NewAppliedModifier.Value;
        if (comp.NewApplyWhenInRange != null)
            pdc.ApplyWhenInRange = comp.NewApplyWhenInRange.Value;
        if (comp.NewLowerBound != null)
            pdc.LowerBound = comp.NewLowerBound.Value;
        if (comp.NewUpperBound != null)
            pdc.UpperBound = comp.NewUpperBound.Value;
    }

    private void OnPressureUpgradeRemoved(Entity<GunUpgradePressureComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        var comp = ent.Comp;
        if (!TryComp<PressureDamageChangeComponent>(args.Container.Owner, out var pdc))
            return;

        pdc.AppliedModifier = comp.SavedAppliedModifier;
        pdc.ApplyWhenInRange = comp.SavedApplyWhenInRange;
        pdc.LowerBound = comp.SavedLowerBound;
        pdc.UpperBound = comp.SavedUpperBound;
    }

    private void OnEffectsUpgradeHit(Entity<WeaponUpgradeEffectsComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var hit in args.HitEntities)
        {
            foreach (var effect in ent.Comp.Effects)
            {
                _entityEffect.Effect(hit, effect, user: ent.Owner);
            }
        }
    }
}
