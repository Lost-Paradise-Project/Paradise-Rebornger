using Content.Shared.Actions;
using Robust.Shared.Containers;
using Content.Shared._LP.Clothing.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Clothing;

namespace Content.Shared._LP.Clothing.Systems;

public abstract class SharedNightVisionSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NightVisionComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<NightVisionComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<NightVisionComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnToggled(Entity<NightVisionComponent> ent, ref ItemToggledEvent args)
    {
        if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            UpdateNightVisionEffects(container.Owner, ent, args.Activated);
    }

    private void OnGotUnequipped(Entity<NightVisionComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        UpdateNightVisionEffects(args.Wearer, ent, false);
    }

    private void OnGotEquipped(Entity<NightVisionComponent> ent, ref ClothingGotEquippedEvent args)
    {
        UpdateNightVisionEffects(args.Wearer, ent, _toggle.IsActivated(ent.Owner));
    }

    protected virtual void UpdateNightVisionEffects(EntityUid user, Entity<NightVisionComponent> ent, bool state) { }
}
