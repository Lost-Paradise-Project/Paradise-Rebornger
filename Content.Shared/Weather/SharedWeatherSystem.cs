using System.Diagnostics.CodeAnalysis;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared._GoobStation.Weather; // Goobstation edit
using Robust.Shared.Serialization; // Goobstation edit

namespace Content.Shared.Weather;

public abstract class SharedWeatherSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly IPrototypeManager ProtoMan = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedRoofSystem _roof = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!; // Goobstation edit

    [Dependency] private readonly EntityQuery<BlockWeatherComponent> _blockQuery = default!;
    [Dependency] private readonly EntityQuery<WeatherStatusEffectComponent> _weatherQuery = default!;

    public static readonly TimeSpan StartupTime = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan ShutdownTime = TimeSpan.FromSeconds(15);

    // Goobstation edit start
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeatherComponent, EntityUnpausedEvent>(OnWeatherUnpaused);
    }
    // Goobstation edit end

    public bool CanWeatherAffect(EntityUid uid, MapGridComponent grid, TileRef tileRef, RoofComponent? roofComp = null) // Goobstation edit
    {
        if (tileRef.Tile.IsEmpty)
            return true;

        if (Resolve(uid, ref roofComp, false) && _roof.IsRooved((uid, grid, roofComp), tileRef.GridIndices)) // Goobstation edit
            return false;

        var tileDef = (ContentTileDefinition)_tileDefManager[tileRef.Tile.TypeId]; // Goobstation edit

        if (!tileDef.Weather)
            return false;

        var anchoredEntities = _mapSystem.GetAnchoredEntitiesEnumerator(uid, grid, tileRef.GridIndices); // Goobstation edit

        while (anchoredEntities.MoveNext(out var ent)) // Goobstation edit
        {
            if (_blockQuery.HasComponent(ent.Value)) // Goobstation edit
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the current “strength” of the specified weather based on the duration of the status effect.
    /// Between 0 and 1.
    /// </summary>
    public float GetWeatherPercent(Entity<StatusEffectComponent> ent)
    {
        var elapsed = Timing.CurTime - ent.Comp.StartEffectTime;
        var duration = ent.Comp.Duration;
        var remaining = duration - elapsed;

        if (remaining < ShutdownTime)
            return (float)(remaining / ShutdownTime);
        else if (elapsed < StartupTime)
            return (float)(elapsed / StartupTime);
        else
            return 1f;
    }

    /// <summary>
    /// Attempts to add a new weather status effect to the specified map.
    /// Does not remove or replace any other existing weather effects on the map.
    /// If the specified weather effect already exists, its duration will be overridden.
    /// </summary>
    /// <param name="mapId">The <see cref="MapId"/> of the target map to apply the weather effect to.</param>
    /// <param name="weatherProto">The prototype ID (<see cref="EntProtoId"/>) of the weather status effect to add.</param>
    /// <param name="weatherEnt">When this method returns, contains the <see cref="EntityUid"/> of the weather entity if the operation succeeded; otherwise, <c>null</c>.</param>
    /// <param name="duration">Optional. The duration for which the weather should exist on the map. If <c>null</c>, the weather will persist indefinitely.</param>
    /// <returns><c>true</c> if the weather was successfully added or updated; otherwise, <c>false</c>.</returns>
    public bool TryAddWeather(MapId mapId, EntProtoId weatherProto, [NotNullWhen(true)] out EntityUid? weatherEnt, TimeSpan? duration = null)
    {
        weatherEnt = null;

        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return false;

        return TryAddWeather(mapUid.Value, weatherProto, out weatherEnt, duration);
    }

    /// <summary>
    /// Adds a new weather to a map. Does not remove other existing weathers. If this type of weather already exists, it simply overrides its duration.
    /// </summary>
    /// <param name="mapUid">Target map entity</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <param name="weatherEnt">When this method returns, contains the <see cref="EntityUid"/> of the weather entity if the operation succeeded; otherwise, <c>null</c>.</param>
    /// <param name="duration">How long this weather should exist on the map? If null - infinite duration</param>
    public bool TryAddWeather(EntityUid mapUid, EntProtoId weatherProto, [NotNullWhen(true)] out EntityUid? weatherEnt, TimeSpan? duration = null)
    {
        return _statusEffects.TrySetStatusEffectDuration(mapUid, weatherProto, out weatherEnt, duration);
    }

    /// <summary>
    /// Checks if a specific weather exists on the given map.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <returns>True if the weather exists, otherwise false</returns>
    public bool HasWeather(MapId mapId, EntProtoId weatherProto)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return false;

        return _statusEffects.TryGetStatusEffect(mapUid.Value, weatherProto, out _);
    }

    /// <summary>
    /// Slowly remove weather from a map. It should be gone after <see cref="ShutdownTime"/> seconds.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    public bool TryRemoveWeather(MapId mapId, EntProtoId weatherProto)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return false;

        return TryRemoveWeather(mapUid.Value, weatherProto);
    }

    /// <summary>
    /// Slowly remove weather from map. It should be gone after <see cref="ShutdownTime"/> seconds.
    /// </summary>
    /// <param name="mapUid">Target entity map</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    public bool TryRemoveWeather(EntityUid mapUid, EntProtoId weatherProto)
    {
        if (!_statusEffects.TryGetStatusEffect(mapUid, weatherProto, out var weatherEnt))
            return false;

        if (!_weatherQuery.HasComp(weatherEnt))
            return false;

        return _statusEffects.TrySetStatusEffectDuration(mapUid, weatherProto, ShutdownTime);
    }

    /// <summary>
    /// Removes all weather conditions except the specified one. If the specified weather does not exist on the map, it adds it.
    /// Returns true if the specified weather is present or was added, false otherwise.
    /// </summary>
    /// <param name="mapId">Target mapId</param>
    /// <param name="weatherProto">EntProtoId of weather status effect</param>
    /// <param name="weatherEnt">When this method returns, contains the <see cref="EntityUid"/> of the weather entity if the operation succeeded; otherwise, <c>null</c>.</param>
    /// <param name="duration">How long this weather should exist on map? If null - infinite duration</param>
    /// <returns><c>true</c> if the specified weather is present or was added; otherwise, <c>false</c>.</returns>
    public bool TrySetWeather(MapId mapId, EntProtoId? weatherProto, out EntityUid? weatherEnt, TimeSpan? duration = null)
    {
        weatherEnt = null;
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return false;

        // Remove all other weather effects except the specified one
        if (_statusEffects.TryEffectsWithComp<WeatherStatusEffectComponent>(mapUid, out var effects))
        {
            foreach (var effect in effects)
            {
                var effectProto = Prototype(effect);
                if (effectProto is null)
                    continue;

                if (effectProto != weatherProto)
                {
                    TryRemoveWeather(mapUid.Value, effectProto);
                }
                else
                {
                    weatherEnt = effect;
                }
            }
        }

        // If weatherProto is null, we just removed all weather and return true
        if (weatherProto is null)
            return true;

        // If the specified weather already exists, just update its duration
        if (weatherEnt != null)
        {
            TryAddWeather(mapUid.Value, weatherProto.Value, out weatherEnt, duration);
            return true;
        }

        // Otherwise, add the specified weather
        return TryAddWeather(mapUid.Value, weatherProto.Value, out weatherEnt, duration);
    }

    // Goobstation edit start

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var curTime = Timing.CurTime;

        var query = EntityQueryEnumerator<WeatherComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Weather.Count == 0)
                continue;

            foreach (var (proto, weather) in comp.Weather)
            {
                var endTime = weather.EndTime;

                // Ended
                if (endTime != null && endTime < curTime)
                {
                    EndWeather(uid, comp, proto);
                    continue;
                }

                var remainingTime = endTime - curTime;

                // Admin messed up or the likes.
                if (!ProtoMan.TryIndex<WeatherPrototype>(proto, out var weatherProto))
                {
                    Log.Error($"Unable to find weather prototype for {comp.Weather}, ending!");
                    EndWeather(uid, comp, proto);
                    continue;
                }

                // Shutting down
                if (endTime != null && remainingTime < WeatherComponent.ShutdownTime)
                {
                    SetState(uid, WeatherState.Ending, comp, weather, weatherProto);
                }
                // Starting up
                else
                {
                    var startTime = weather.StartTime;
                    var elapsed = Timing.CurTime - startTime;

                    if (elapsed < WeatherComponent.StartupTime)
                    {
                        SetState(uid, WeatherState.Starting, comp, weather, weatherProto);
                    }
                    // Begin DeltaV: Set state to Running when it finishes the starting time
                    else
                        SetState(uid, WeatherState.Running, comp, weather, weatherProto);
                    // End DeltaV
                }

                // Run whatever code we need.
                Run(uid, weather, weatherProto, frameTime);
            }
        }
    }

    private void OnWeatherUnpaused(EntityUid uid, WeatherComponent component, ref EntityUnpausedEvent args)
    {
        foreach (var weather in component.Weather.Values)
        {
            weather.StartTime += args.PausedTime;

            if (weather.EndTime != null)
                weather.EndTime = weather.EndTime.Value + args.PausedTime;
        }
        component.NextUpdate += args.PausedTime; // DeltaV
    }

    /// <summary>
    /// Run every tick when the weather is running.
    /// </summary>
    protected virtual void Run(EntityUid uid, WeatherData weather, WeatherPrototype weatherProto, float frameTime) { }

    protected virtual void EndWeather(EntityUid uid, WeatherComponent component, string proto)
    {
        if (!component.Weather.TryGetValue(proto, out var data))
            return;

        Audio.Stop(data.Stream);
        data.Stream = null;
        component.Weather.Remove(proto);
        Dirty(uid, component);
    }

    protected virtual bool SetState(EntityUid uid, WeatherState state, WeatherComponent component, WeatherData weather, WeatherPrototype weatherProto)
    {
        if (weather.State.Equals(state))
            return false;

        weather.State = state;
        Dirty(uid, component);
        return true;
    }

    /// <summary>
    /// Shuts down all existing weather and starts the new one if applicable.
    /// </summary>
    public void SetWeather(MapId mapId, WeatherPrototype? proto, TimeSpan? endTime)
    {
        if (!_mapSystem.TryGetMap(mapId, out var mapUid))
            return;

        var weatherComp = EnsureComp<WeatherComponent>(mapUid.Value);

        foreach (var (eProto, weather) in weatherComp.Weather)
        {
            // if we turn off the weather, we don't want endTime = null
            if (proto == null)
                endTime ??= Timing.CurTime + WeatherComponent.ShutdownTime;

            // Reset cooldown if it's an existing one.
            if (proto is not null && eProto == proto.ID)
            {
                weather.EndTime = endTime;
                if (weather.State == WeatherState.Ending)
                    weather.State = WeatherState.Running;

                Dirty(mapUid.Value, weatherComp);
                continue;
            }

            // Speedrun
            var end = Timing.CurTime + WeatherComponent.ShutdownTime;

            if (weather.EndTime == null || weather.EndTime > end)
            {
                weather.EndTime = end;
                Dirty(mapUid.Value, weatherComp);
            }
        }

        if (proto != null)
            StartWeather(mapUid.Value, weatherComp, proto, endTime);
    }

    protected void StartWeather(EntityUid uid, WeatherComponent component, WeatherPrototype weather, TimeSpan? endTime)
    {
        if (component.Weather.ContainsKey(weather.ID))
            return;

        var data = new WeatherData()
        {
            StartTime = Timing.CurTime,
            EndTime = endTime,
        };

        component.Weather.Add(weather.ID, data);
        Dirty(uid, component);
    }

    [Serializable, NetSerializable]
    protected sealed class WeatherComponentState : ComponentState
    {
        public Dictionary<ProtoId<WeatherPrototype>, WeatherData> Weather;

        public WeatherComponentState(Dictionary<ProtoId<WeatherPrototype>, WeatherData> weather)
        {
            Weather = weather;
        }
    }

    public float GetPercent(WeatherData component, EntityUid mapUid)
    {
        var pauseTime = _metadata.GetPauseTime(mapUid);
        var elapsed = Timing.CurTime - (component.StartTime + pauseTime);
        var duration = component.Duration;
        var remaining = duration - elapsed;
        float alpha;

        if (remaining < WeatherComponent.ShutdownTime)
        {
            alpha = (float) (remaining / WeatherComponent.ShutdownTime);
        }
        else if (elapsed < WeatherComponent.StartupTime)
        {
            alpha = (float) (elapsed / WeatherComponent.StartupTime);
        }
        else
        {
            alpha = 1f;
        }

        return alpha;
    }

    // Goobstation edit end

}
