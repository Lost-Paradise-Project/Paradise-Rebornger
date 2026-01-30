using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._DEN.CCVar;

public sealed class DCCVars : CVars
{
    /// <summary>
    /// URL of the Discord webhook which will relay all ahelp messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordFaxChannelId =
        CVarDef.Create("discord.fax_channel_id", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
