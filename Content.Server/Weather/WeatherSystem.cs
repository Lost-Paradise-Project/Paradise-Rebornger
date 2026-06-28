using Content.Shared.Weather;
using Robust.Server.GameStates;
using Content.Shared._GoobStation.Weather; // Goobstation edit
using Robust.Shared.GameStates; // Goobstation edit

namespace Content.Server.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    //I dont really like to PVS override weather entities, but map status effect containers dont PVS-ing out of the box
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    public override void Initialize()
    {
        base.Initialize(); // Goobstation edit

        SubscribeLocalEvent<WeatherStatusEffectComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<WeatherStatusEffectComponent, ComponentShutdown>(OnCompShutdown);
        SubscribeLocalEvent<WeatherComponent, ComponentGetState>(OnWeatherGetState); // Goobstation edit
    }

    // Goobstation edit start
    private void OnWeatherGetState(EntityUid uid, WeatherComponent component, ref ComponentGetState args)
    {
        args.State = new WeatherComponentState(component.Weather);
    }
    // Goobstation edit end

    private void OnCompInit(Entity<WeatherStatusEffectComponent> ent, ref ComponentInit args)
    {
        // The map entitiy itself is networked by PVS if the player is on that map but not anything inside a container,
        // So we need to add an overridce to make sure the client sees it.
        _pvs.AddGlobalOverride(ent);
    }

    private void OnCompShutdown(Entity<WeatherStatusEffectComponent> ent, ref ComponentShutdown args)
    {
        _pvs.RemoveGlobalOverride(ent);
    }
}
