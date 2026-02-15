using Content.Shared.Goobstation.Singularity;

namespace Content.Server.Goobstation.Singularity.ContainmentField;

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
