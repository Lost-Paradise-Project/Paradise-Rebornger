using Content.Shared._GoobStation.Interactions;
using Content.Server._GoobStation.Interaction.Components;
using Content.Server.Popups;
using Content.Server.Station.Systems;

namespace Content.Server._GoobStation.Interaction.Systems;

public sealed partial class UseOnStationOnlySystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseOnStationOnlyComponent, UseInHandAttemptEvent>(OnUseAttempt);
    }

    private void OnUseAttempt(Entity<UseOnStationOnlyComponent> item, ref UseInHandAttemptEvent args)
    {
        if (_station.GetOwningStation(args.User) is not null)
            return;

        _popup.PopupEntity(Loc.GetString("use-on-station-only-not-on-station"), args.User, args.User);
        args.Cancel();
    }
}
