using Content.Server.Administration;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._GoobStation.Administration;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
// LP edit start
using System.Linq;
using System.Threading.Tasks;
// LP edit end

namespace Content.Server._GoobStation.Administration;

public sealed class TimeTransferPanelEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminMan = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IServerDbManager _databaseMan = default!;
    // LP edit start
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    // LP edit end

    private readonly ISawmill _sawmill;

    public TimeTransferPanelEui()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("admin.time_eui");
    }

    public override TimeTransferPanelEuiState GetNewState()
    {
        var hasFlag = _adminMan.HasAdminFlag(Player, AdminFlags.Playtime); // Corvax-DiscordRoles

        return new TimeTransferPanelEuiState(hasFlag);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not TimeTransferEuiMessage message)
            return;

        TransferTime(message.PlayerId, message.TimeData, message.Overwrite);
    }

    public async void TransferTime(string playerId, List<TimeTransferData> timeData, bool overwrite)
    {
        if (!_adminMan.HasAdminFlag(Player, AdminFlags.Playtime)) // Corvax-DiscordRoles
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId} tried to add roles time without moderator flag)");
            return;
        }

        var playerData = await _playerLocator.LookupIdByNameAsync(playerId);
        if (playerData == null)
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId} tried to add roles time to not existing player {playerId})");
            SendMessage(new TimeTransferWarningEuiMessage(Loc.GetString("time-transfer-panel-no-player-database-message"), Color.Red));
            return;
        }

        // LP edit start
        if (overwrite)
            await SetTime(playerData.UserId, timeData);
        else
            await AddTime(playerData.UserId, timeData);
        // LP edit end
    }

// LP edit start(rewrite funcs SetTime() and AddTime() plz god help me)

    public async Task SetTime(NetUserId userId, List<TimeTransferData> timeData)
    {
        if (!_playerManager.TryGetSessionById(userId, out var player))
        {
            _sawmill.Warning($"Could not find session for user {userId}");
            SendMessage(new TimeTransferWarningEuiMessage(Loc.GetString("time-transfer-panel-warning-player-not-online"), Color.Orange));
            return;
        }

        var updateList = new List<PlayTimeUpdate>();

        foreach (var data in timeData)
        {
            if (data.PlaytimeTracker == "Overall")
            {
                var newOverall = TimeSpan.FromMinutes(PlayTimeCommandUtilities.CountMinutes(data.TimeString));
                var currentOverall = _playTimeTracking.GetOverallPlaytime(player);
                var diff = newOverall - currentOverall;

                if (diff != TimeSpan.Zero)
                {
                    if (diff < TimeSpan.Zero)
                    {
                        SendMessage(new TimeTransferWarningEuiMessage(Loc.GetString("time-transfer-panel-warning-decrease-not-supported"), Color.Orange));
                    }
                    _playTimeTracking.AddTimeToOverallPlaytime(player, diff);
                } continue;
            }

            var time = TimeSpan.FromMinutes(PlayTimeCommandUtilities.CountMinutes(data.TimeString));
            updateList.Add(new PlayTimeUpdate(userId, data.PlaytimeTracker, time));
        }

        if (updateList.Count > 0)
            await _databaseMan.UpdatePlayTimes(updateList);

        _sawmill.Info($"{Player.Name} ({Player.UserId} saved {updateList.Count} trackers for {userId})");

        SendMessage(new TimeTransferWarningEuiMessage(Loc.GetString("time-transfer-panel-warning-set-success"), Color.LightGreen));
    }

    public async Task AddTime(NetUserId userId, List<TimeTransferData> timeData)
    {
        if (!_playerManager.TryGetSessionById(userId, out var player))
        {
            _sawmill.Warning($"Could not find session for user {userId}");
            SendMessage(new TimeTransferWarningEuiMessage(Loc.GetString("time-transfer-panel-warning-player-not-online"), Color.Orange));
            return;
        }

        var playTimeList = await _databaseMan.GetPlayTimes(userId);
        var playTimeDict = playTimeList.ToDictionary(pt => pt.Tracker, pt => pt.TimeSpent);
        var updateList = new List<PlayTimeUpdate>();

        foreach (var data in timeData)
        {
            var addMinutes = PlayTimeCommandUtilities.CountMinutes(data.TimeString);
            var addTime = TimeSpan.FromMinutes(addMinutes);

            if (data.PlaytimeTracker == "Overall")
            {
                if (addTime != TimeSpan.Zero)
                {
                    _playTimeTracking.AddTimeToOverallPlaytime(player, addTime);
                } continue;
            }

            if (playTimeDict.TryGetValue(data.PlaytimeTracker, out var existing))
                addTime += existing;

            updateList.Add(new PlayTimeUpdate(userId, data.PlaytimeTracker, addTime));
        }

        if (updateList.Count > 0)
            await _databaseMan.UpdatePlayTimes(updateList);

        _sawmill.Info($"{Player.Name} ({Player.UserId} saved {updateList.Count} trackers for {userId})");

        SendMessage(new TimeTransferWarningEuiMessage(Loc.GetString("time-transfer-panel-warning-add-success"), Color.LightGreen));
    }
// LP edit end

    public override async void Opened()
    {
        base.Opened();
        _adminMan.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _adminMan.OnPermsChanged -= OnPermsChanged;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
        {
            return;
        }

        StateDirty();
    }
}
