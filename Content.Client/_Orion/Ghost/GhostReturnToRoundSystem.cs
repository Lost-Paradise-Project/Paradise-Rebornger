using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared._Orion.Ghost;
using Content.Shared.Ghost;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._Orion.Ghost;

public sealed class GhostReturnToRoundSystem : SharedGhostReturnToRoundSystem
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private TimeSpan _lastTimeLeft = TimeSpan.Zero;
    /// LP edit start
    /// <summary>
    /// The moment when the player will be able to return.
    /// Момент времени, когда игрок смогёт вернуться.
    /// </summary>
    private TimeSpan _returnToRoundTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostReturnToRoundSendTimerEvent>(GetReturnToRoundTimer);
    }

    private void GetReturnToRoundTimer(GhostReturnToRoundSendTimerEvent ev)
    {
        _returnToRoundTime = ev.ReturnTime;
        _lastTimeLeft = TimeSpan.Zero;
    }
    /// LP edit end

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null)
            return;

        /// LP edit start
        // Not required.
        // Этот пристройка тут нахой не нужен.
        //
        // if (!TryComp<GhostComponent>(player, out var ghostComponent))
        //     return;
        /// LP edit end

        var ui = _userInterfaceManager.GetActiveUIWidgetOrNull<GhostGui>();
        if (ui == null)
            return;

        /// LP edit start
        // var timeOffset = _gameTiming.CurTime - ghostComponent.TimeOfDeath;
        // var rawTimeLeft = GhostRespawnTime - timeOffset;
        ///

        var rawTimeLeft = _returnToRoundTime - _gameTiming.CurTime;
        /// LP edit end
        var timeLeft = rawTimeLeft > TimeSpan.Zero ? rawTimeLeft : TimeSpan.Zero;
        var canReturn = timeLeft == TimeSpan.Zero;

        var displayTime = FormatTimeLeft(timeLeft);

        var buttonStateChanged = ui.ReturnToRound.Disabled == canReturn;
        var timeChanged = FormatTimeLeft(_lastTimeLeft) != displayTime;

        if (!buttonStateChanged && !timeChanged)
            return;

        ui.ReturnToRound.Disabled = !canReturn;
        ui.ReturnToRound.Text = canReturn
            ? Loc.GetString("ghost-gui-return-to-round-ready-button")
            : Loc.GetString("ghost-gui-return-to-round-button", ("time", displayTime));

        _lastTimeLeft = timeLeft;
    }

    private static string FormatTimeLeft(TimeSpan timeLeft)
    {
        var totalMinutes = (int)timeLeft.TotalMinutes; /// LP edit
        var seconds = timeLeft.Seconds;

        return $"{totalMinutes:00}:{seconds:00}";
    }
}
