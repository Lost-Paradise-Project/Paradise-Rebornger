using Robust.Shared.Configuration;

namespace Content.Shared._Orion.CCVar;

public sealed partial class OCCVars
{
    /*
     * Ghost Respawn
     */

    public static readonly CVarDef<float> GhostRespawnTime =
        CVarDef.Create("ghost.respawn_time", 300f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> GhostRespawnMaxPlayers =
        CVarDef.Create("ghost.respawn_max_players", 80, CVar.SERVERONLY);
}
