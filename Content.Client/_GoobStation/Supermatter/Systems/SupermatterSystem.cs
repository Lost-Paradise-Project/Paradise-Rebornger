using Content.Shared._GoobStation.Supermatter.Components;
using Content.Shared._GoobStation.Supermatter.Systems;
using Robust.Shared.GameStates;

namespace Content.Client._GoobStation.Supermatter.Systems;

public sealed class SupermatterSystem : SharedSupermatterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterComponent, ComponentHandleState>(HandleSupermatterState);
    }

    private void HandleSupermatterState(EntityUid uid, SupermatterComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not SupermatterComponentState state)
            return;
    }
}
