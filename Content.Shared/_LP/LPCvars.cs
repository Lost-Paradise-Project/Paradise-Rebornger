using Robust.Shared.Configuration;

namespace Content.Shared._LP;

[CVarDefs]
public sealed class LPCvars
{
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", "", CVar.SERVERONLY);


    #region Cheats
    /// <summary>
    /// Переключатель читов.
    /// </summary>
    // public static readonly CVarDef<bool> EnableCheats =
    //     CVarDef.Create("cheats.enabled", false, CVar.SERVER | CVar.CHEAT);

    /// <summary>
    /// Позволяет указать автоматически выдаваемый ВСЕМ игрокам уровень спонсорки.
    /// Использовать только для тестирования.
    /// </summary>
    public static readonly CVarDef<int> SponsorLevelHack =
        CVarDef.Create("cheats.sponsorlevel_hack", 0, CVar.REPLICATED | CVar.CHEAT | CVar.NOTIFY);
    #endregion
}
