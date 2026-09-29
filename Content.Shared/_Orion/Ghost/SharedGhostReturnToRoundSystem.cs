using Content.Shared._Orion.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Serialization; /// LP edit

namespace Content.Shared._Orion.Ghost;

public abstract class SharedGhostReturnToRoundSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager Cfg = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        Cfg.OnValueChanged(OCCVars.GhostRespawnTime,
            ghostRespawnTime =>
            {
                GhostRespawnTime = TimeSpan.FromSeconds(ghostRespawnTime);
            },
            true);
    }

    protected TimeSpan GhostRespawnTime = new(0, 5, 0);
}

/// LP edit start
/// <summary>
/// It is responsible for transmitting to the player the time from the server when they can return to the round.
/// Отвечает за передачу игроку времени с сервера, когда тому можно вернуться в раунд.
/// </summary>
[Serializable, NetSerializable]
public sealed class GhostReturnToRoundSendTimerEvent(TimeSpan returnTime) : EntityEventArgs
{
    public TimeSpan ReturnTime { get; } = returnTime;
}

/// <summary>
/// The server notifies that the return to round timer needs to be started for a specific player.
/// Уведомляет сервер о том, что надо запустить таймер возврата в раунд для конкретного игрока.
/// </summary>
public sealed class GhostReturnToRoundTimerStartEvent(EntityUid ghost) : EntityEventArgs
{
    public EntityUid Ghost { get; } = ghost;
}
/// LP edit end
