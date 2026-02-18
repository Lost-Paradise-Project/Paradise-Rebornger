using Content.Server.Singularity.Events;
using Content.Shared.Whitelist;

namespace Content.Server._GoobStation.Singularity.EventHorizon;

public sealed class EventHorizonIgnoreSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventHorizonIgnoreComponent, EventHorizonAttemptConsumeEntityEvent>(OnAttemptConsume);
    }

    private void OnAttemptConsume(Entity<EventHorizonIgnoreComponent> ent, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        if (_whitelist.IsWhitelistPass(ent.Comp.HorizonWhitelist, args.EventHorizonUid)) // LP Edit IsBlacklistFailOrNull -> IsWhitelistPass
            args.Cancelled = true;
    }
}
