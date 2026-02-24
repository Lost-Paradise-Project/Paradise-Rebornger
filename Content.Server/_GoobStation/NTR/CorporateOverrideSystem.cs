using Content.Shared.Store.Components;
using Robust.Shared.Containers;

namespace Content.Server._GoobStation.NTR;

public sealed class CorporateOverrideSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CorporateOverrideComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CorporateOverrideComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<CorporateOverrideComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
    }

    private void OnInit(EntityUid uid, CorporateOverrideComponent comp, ComponentInit args) =>
        comp.OverrideSlot = _container.EnsureContainer<ContainerSlot>(uid, CorporateOverrideComponent.ContainerId);

    private void OnItemInserted(EntityUid uid, CorporateOverrideComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != CorporateOverrideComponent.ContainerId
            || !TryComp<StoreComponent>(uid, out var store))
            return;

        // if (store.Categories.Add(comp.UnlockedCategory))
        Dirty(uid, store);
    }

    private void OnItemRemoved(EntityUid uid, CorporateOverrideComponent comp, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != CorporateOverrideComponent.ContainerId
            || !TryComp<StoreComponent>(uid, out var store))
            return;

        // if (!store.Categories.Contains(comp.UnlockedCategory))
        //     return;

        // store.Categories.Remove(comp.UnlockedCategory);
        Dirty(uid, store);
    }
}
