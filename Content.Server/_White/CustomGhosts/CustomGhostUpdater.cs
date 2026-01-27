using Robust.Shared.Network;
using Content.Shared._White.CustomGhostSystem;
using Content.Server.Database;
using Robust.Shared.Prototypes;
using Content.Server.Preferences.Managers;
using Robust.Server.Player;
using Robust.Shared.Player;
using Content.Shared.Players;
using Content.Server._LP.Sponsors;
using Robust.Shared.Serialization;

namespace Content.Server._LP.CustomGhostSystem;   //LP edit: сделано в дополнение к wwdp customghosts

public sealed class CustomGhostUpdater : EntitySystem
{
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefMan = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        _netManager.RegisterNetMessage<ChangeCustomGhostMsg>(OnCustomGhostCheck);
        //_netManager.RegisterNetMessage<CustomGhostAnswer>();
    }

    private void OnCustomGhostCheck(ChangeCustomGhostMsg ev)
    {
        var protoId = ev.id;
        var player = (NetUserId)Guid.Parse(ev.uuid);

        if (!_proto.TryIndex<CustomGhostPrototype>(protoId, out var proto))
            return;

        if (!proto.CanUse(_playerManager.GetSessionById(player), SponsorSimpleManager.GetTier(player))) //LP edit
            return;


        _db.SaveGhostTypeAsync(player, protoId);
        var prefs = _prefMan.GetPreferences(player);
        prefs.CustomGhost = protoId;
    }
}
