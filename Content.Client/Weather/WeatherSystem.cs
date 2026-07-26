using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Content.Shared._GoobStation.Weather; // Goobstation edit
using Robust.Shared.GameStates; // Goobstation edit

namespace Content.Client.Weather;

public sealed partial class WeatherSystem : SharedWeatherSystem // Goob edit - made partial
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    [Dependency] private readonly EntityQuery<AudioComponent> _audioQuery = default!;
    [Dependency] private readonly EntityQuery<MapGridComponent> _gridQuery = default!;
    [Dependency] private readonly EntityQuery<RoofComponent> _roofQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeatherStatusEffectComponent, ComponentShutdown>(OnComponentShutdown);
        // Goobstation edit start
        SubscribeLocalEvent<WeatherComponent, ComponentHandleState>(OnWeatherHandleState);
    }

    protected override void Run(EntityUid uid, WeatherData weather, WeatherPrototype weatherProto, float frameTime)
    {
        base.Run(uid, weather, weatherProto, frameTime);

        var ent = _playerManager.LocalEntity;

        if (ent == null)
            return;

        var mapUid = Transform(uid).MapUid;
        var entXform = Transform(ent.Value);

        // Maybe have the viewports manage this?
        if (mapUid == null || entXform.MapUid != mapUid)
        {
            weather.Stream = _audio.Stop(weather.Stream);
            return;
        }

        if (!Timing.IsFirstTimePredicted || weatherProto.Sound == null)
            return;

        weather.Stream ??= _audio.PlayGlobal(weatherProto.Sound, Filter.Local(), true)?.Entity;

        if (!TryComp(weather.Stream, out AudioComponent? comp))
            return;

        var occlusion = 0f;

        // Work out tiles nearby to determine volume.
        if (TryComp<MapGridComponent>(entXform.GridUid, out var grid))
        {
            TryComp(entXform.GridUid, out RoofComponent? roofComp);
            var gridId = entXform.GridUid.Value;
            // FloodFill to the nearest tile and use that for audio.
            var seed = _mapSystem.GetTileRef(gridId, grid, entXform.Coordinates);
            var frontier = new Queue<TileRef>();
            frontier.Enqueue(seed);
            // If we don't have a nearest node don't play any sound.
            EntityCoordinates? nearestNode = null;
            var visited = new HashSet<Vector2i>();

            while (frontier.TryDequeue(out var node))
            {
                if (!visited.Add(node.GridIndices))
                    continue;

                if (!CanWeatherAffect(entXform.GridUid.Value, grid, node, roofComp))
                {
                    // Add neighbors
                    // TODO: Ideally we pick some deterministically random direction and use that
                    // We can't just do that naively here because it will flicker between nearby tiles.
                    for (var x = -1; x <= 1; x++)
                    {
                        for (var y = -1; y <= 1; y++)
                        {
                            if (Math.Abs(x) == 1 && Math.Abs(y) == 1 ||
                                x == 0 && y == 0 ||
                                (new Vector2(x, y) + node.GridIndices - seed.GridIndices).Length() > 3)
                            {
                                continue;
                            }

                            frontier.Enqueue(_mapSystem.GetTileRef(gridId, grid, new Vector2i(x, y) + node.GridIndices));
                        }
                    }

                    continue;
                }

                nearestNode = new EntityCoordinates(entXform.GridUid.Value,
                    node.GridIndices + grid.TileSizeHalfVector);
                break;
            }

            // Get occlusion to the targeted node if it exists, otherwise set a default occlusion.
            if (nearestNode != null)
            {
                var entPos = _transform.GetMapCoordinates(entXform);
                var nodePosition = _transform.ToMapCoordinates(nearestNode.Value).Position;
                var delta = nodePosition - entPos.Position;
                var distance = delta.Length();
                occlusion = _audio.GetOcclusion(entPos, delta, distance);
            }
            else
            {
                occlusion = 3f;
            }
        }

        var alpha = GetPercent(weather, uid);
        alpha *= SharedAudioSystem.VolumeToGain(weatherProto.Sound.Params.Volume);
        _audio.SetGain(weather.Stream, alpha, comp);
        comp.Occlusion = occlusion;
    }

    protected override bool SetState(EntityUid uid, WeatherState state, WeatherComponent comp, WeatherData weather, WeatherPrototype weatherProto)
    {
        if (!base.SetState(uid, state, comp, weather, weatherProto))
            return false;

        if (!Timing.IsFirstTimePredicted)
            return true;

        // Begin DeltaV Additions: Prevent hearing weather in the lobby
        if (!CanHearWeather(uid, weather))
            return false;
        // End DeltaV Additions

        // TODO: Fades (properly)
        weather.Stream = _audio.Stop(weather.Stream);
        weather.Stream = _audio.PlayGlobal(weatherProto.Sound, Filter.Local(), true)?.Entity;
        return true;
    }

    private void OnWeatherHandleState(EntityUid uid, WeatherComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not WeatherComponentState state)
            return;

        foreach (var (proto, weather) in component.Weather)
        {
            // End existing one
            if (!state.Weather.TryGetValue(proto, out var stateData))
            {
                EndWeather(uid, component, proto);
                continue;
            }

            // Data update?
            weather.StartTime = stateData.StartTime;
            weather.EndTime = stateData.EndTime;
            weather.State = stateData.State;
        }

        foreach (var (proto, weather) in state.Weather)
        {
            if (component.Weather.ContainsKey(proto))
                continue;

            // New weather
            StartWeather(uid, component, ProtoMan.Index<WeatherPrototype>(proto), weather.EndTime);
        }
        // Goobstation edit end
    }

    private void OnComponentShutdown(Entity<WeatherStatusEffectComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Stream = _audio.Stop(ent.Comp.Stream);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var player = _playerManager.LocalEntity;

        if (player == null)
            return;

        var playerXform = Transform(player.Value);

        var query = EntityQueryEnumerator<WeatherStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var weather, out var status))
        {
            if (weather.Sound == null || status.AppliedTo != playerXform.MapUid)
            {
                weather.Stream = _audio.Stop(weather.Stream);
                return;
            }

            weather.Stream ??= _audio.PlayGlobal(weather.Sound, Filter.Local(), true)?.Entity;

            if (!_audioQuery.TryComp(weather.Stream, out var audio))
                return;

            var occlusion = 0f;

            // Work out tiles nearby to determine volume.
            if (_gridQuery.TryComp(playerXform.GridUid, out var grid))
            {
                _roofQuery.TryComp(playerXform.GridUid, out var roofComp);
                var gridId = playerXform.GridUid.Value;
                // FloodFill to the nearest tile and use that for audio.
                var seed = _mapSystem.GetTileRef(gridId, grid, playerXform.Coordinates);
                var frontier = new Queue<TileRef>();
                frontier.Enqueue(seed);
                // If we don't have a nearest node don't play any sound.
                EntityCoordinates? nearestNode = null;
                var visited = new HashSet<Vector2i>();

                while (frontier.TryDequeue(out var node))
                {
                    if (!visited.Add(node.GridIndices))
                        continue;

                    if (!CanWeatherAffect(playerXform.GridUid.Value, grid, node, roofComp)) // Goobstation edit
                    {
                        // Add neighbors
                        // TODO: Ideally we pick some deterministically random direction and use that
                        // We can't just do that naively here because it will flicker between nearby tiles.
                        for (var x = -1; x <= 1; x++)
                        {
                            for (var y = -1; y <= 1; y++)
                            {
                                if (Math.Abs(x) == 1 && Math.Abs(y) == 1 ||
                                    x == 0 && y == 0 ||
                                    (new Vector2(x, y) + node.GridIndices - seed.GridIndices).Length() > 3)
                                {
                                    continue;
                                }

                                frontier.Enqueue(_mapSystem.GetTileRef(gridId, grid, new Vector2i(x, y) + node.GridIndices));
                            }
                        }

                        continue;
                    }

                    nearestNode = new EntityCoordinates(playerXform.GridUid.Value,
                        node.GridIndices + grid.TileSizeHalfVector);
                    break;
                }

                // Get occlusion to the targeted node if it exists, otherwise set a default occlusion.
                if (nearestNode != null)
                {
                    var entPos = _transform.GetMapCoordinates(playerXform);
                    var nodePosition = _transform.ToMapCoordinates(nearestNode.Value).Position;
                    var delta = nodePosition - entPos.Position;
                    var distance = delta.Length();
                    occlusion = _audio.GetOcclusion(entPos, delta, distance);
                }
                else
                {
                    occlusion = 3f;
                }
            }

            var alpha = GetWeatherPercent((uid, status));
            alpha *= SharedAudioSystem.VolumeToGain(weather.Sound.Params.Volume);
            _audio.SetGain(weather.Stream, alpha, audio);
            audio.Occlusion = occlusion;
        }
    }
}
