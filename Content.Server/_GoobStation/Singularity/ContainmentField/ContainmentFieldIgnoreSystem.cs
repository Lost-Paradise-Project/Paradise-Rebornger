using Content.Shared._GoobStation.Singularity;

namespace Content.Server._GoobStation.Singularity.ContainmentField;

public sealed class ContainmentFieldIgnoreSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContainmentFieldIgnoreComponent, ContainmentFieldThrowEvent>(OnThrow);
    }

    private void OnThrow(Entity<ContainmentFieldIgnoreComponent> ent, ref ContainmentFieldThrowEvent args)
    {
        args.Cancelled = true;
    }
}
