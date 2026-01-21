using Robust.Shared.Configuration;

namespace Content.Shared._Wega.CCCVars;

[CVarDefs]
public sealed class WegaCVars
{
    /*
    Night Light System CVars
*/
    /// <summary>
    /// Responsible for switching the night light system.
    /// </summary>
    public static readonly CVarDef<bool> NightLightEnabled =
        CVarDef.Create("wega.night_light_enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Switching adjusts all the lamps to the holiday mode according to the logic of updating the night lighting.
    /// </summary>
    public static readonly CVarDef<bool> PartyEnabled =
        CVarDef.Create("wega.party_enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);
}
