using Robust.Shared.Configuration;

namespace Content.Shared._ADT.CCVar;

[CVarDefs]
public sealed class ADTCCVars
{

    /*
     * Headshot
     */
    // public static readonly CVarDef<string> HeadshotUrl =
    // CVarDef.Create("ic.headshot_url", "https://discord.com/channels/901772674865455115/1446603657255850085", CVar.SERVER | CVar.REPLICATED);
    public static readonly CVarDef<string> HeadshotDomain =
        CVarDef.Create("ic.headshot_domain", "i.pinimg.com", CVar.SERVER | CVar.REPLICATED);

}
