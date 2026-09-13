using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._Orion.CCVar;
using Content.Shared._Orion.Ghost;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Timing; /// LP edit

namespace Content.Server._Orion.Ghost;

public sealed class GhostReturnToRoundSystem : SharedGhostReturnToRoundSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    /// LP edit start
    // [Dependency] private readonly SharedGhostSystem _ghostSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    /// LP edit end

    private int _ghostRespawnMaxPlayers;
    private readonly Dictionary<EntityUid, TimeSpan> _returnToRoundTimes = []; /// LP edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostReturnToRoundRequest>(OnGhostReturnToRoundRequest);
        SubscribeLocalEvent<GhostReturnToRoundTimerStartEvent>(ReturnToRoundTimer); /// LP edit

        Cfg.OnValueChanged(OCCVars.GhostRespawnMaxPlayers,
            ghostRespawnMaxPlayers =>
            {
                _ghostRespawnMaxPlayers = ghostRespawnMaxPlayers;
            },
            true);

        _console.RegisterCommand("returntoround", ReturnToRoundCommand, ReturnToRoundCompletion);
    }

    /// LP edit start
    /// <summary>
    /// The timer for the player to return to the lobby.
    /// Таймер возврата игрока в лобби.
    /// </summary>
    private void ReturnToRoundTimer(GhostReturnToRoundTimerStartEvent ev)
    {
        var timeFromStartRound = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        // A little bit of offset won’t hurt, since that is what the founding fathers intended. Немного смещения не помешает, ведь именно так и задумывали отцы-основатели).
        var timeOffset = TimeSpan.Zero;
        if (timeFromStartRound > TimeSpan.FromHours(1)) // The time offset begins with the first hour of the shift. Смещение времени начинается с одного часа смены.
            timeOffset = TimeSpan.FromHours(Math.Min((timeFromStartRound.TotalHours - 1) / 3 * 10, 10));

        var returnTime = GameTiming.CurTime + GhostRespawnTime + timeOffset; // We calculate the time when the player can respawn. Вычисляем время, когда игрок может зареспавниться.
        _returnToRoundTimes[ev.Ghost] = returnTime; // Save the time on specific ghost. Сохраняем время на конкретного призрака.

        if (!_playerManager.TryGetSessionByEntity(ev.Ghost, out var session)) // We are looking for the player who owns the ghost. Шукаемо игрока, которому принадлежит призрак.
            return;

        RaiseNetworkEvent(new GhostReturnToRoundSendTimerEvent(returnTime), session.Channel); // Found it, sending the time to the player. Нашли, отправляем время игроку.
    }
    /// LP edit end

    private void TryGhostReturnToRound(EntityUid uid, Entity<GhostComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!_playerManager.TryGetSessionByEntity(uid, out var session))
            return;

        if (_playerManager.PlayerCount >= _ghostRespawnMaxPlayers)
        {
            SendChatMsg(session,
                Loc.GetString("ghost-respawn-max-players", ("players", _ghostRespawnMaxPlayers))
            );
            return;
        }

        /// LP edit start
        // var now = GameTiming.CurTime;
        // var timeOffset = now - ent.Comp.TimeOfDeath;

        // if (timeOffset < TimeSpan.Zero)
        // {
        //     Entity<GhostComponent?> ghostEnt = (ent.Owner, ent.Comp);
        //     _ghostSystem.SetTimeOfDeath(ghostEnt, now);
        //     timeOffset = TimeSpan.Zero;
        // }

        // if (timeOffset < GhostRespawnTime)

        if (!_returnToRoundTimes.TryGetValue(uid, out var returnTime))
            return;

        var timeLeft = returnTime - GameTiming.CurTime;

        if (timeLeft > TimeSpan.Zero)
        /// LP edit end
        {
            SendChatMsg(session,
            Loc.GetString("ghost-respawn-time-left", ("time", timeLeft.ToString())) /// LP edit (GhostRespawnTime - timeOffset) > timeLeft
            );
            return;
        }

        _returnToRoundTimes.Remove(uid); /// LP edit
        _gameTicker.Respawn(session);
        _adminLogger.Add(LogType.Mind, LogImpact.Medium, $"{Loc.GetString("ghost-respawn-log-return-to-lobby", ("userName", session.Name))}");

        var message = Loc.GetString("ghost-respawn-window-rules-footer"); /// LP edit
        SendChatMsg(session, message);
    }

    private void OnGhostReturnToRoundRequest(GhostReturnToRoundRequest msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ghost)
            return;

        if (!TryComp<GhostComponent>(ghost, out var ghostComponent))
            return;

        TryGhostReturnToRound(ghost, (ghost, ghostComponent));
    }

    private static CompletionResult ReturnToRoundCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }

    [AnyCommand]
    private void ReturnToRoundCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (shell.Player?.AttachedEntity is not { } ghost || !TryComp<GhostComponent>(ghost, out var ghostComponent))
        {
            shell.WriteError(Loc.GetString("ghost-respawn-command-no-entity"));
            return;
        }

        TryGhostReturnToRound(ghost, (ghost, ghostComponent));
    }

    private void SendChatMsg(ICommonSession session, string message)
    {
        _chatManager.ChatMessageToOne(ChatChannel.Server,
            message,
            Loc.GetString("chat-manager-server-wrap-message", ("message", message)),
            default,
            false,
            session.Channel,
            Color.Red);
    }
}
