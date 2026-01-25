using Robust.Shared.Configuration;

namespace Content.Shared._LP;

[CVarDefs]
public sealed class LPCvars
{
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", "", CVar.SERVERONLY);
}
