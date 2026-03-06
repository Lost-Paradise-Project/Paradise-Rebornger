using Robust.Shared.Configuration;

namespace Content.Shared._LP;

[CVarDefs]
public sealed class LPCvars
{
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", "", CVar.SERVERONLY);

    /// <summary>
    /// Режим просмотра в редакторе персонажа.
    /// "Rotate" - кнопки поворота (по умолчанию)
    /// "List" - вертикальный список из 4 спрайтов
    /// </summary>
    public static readonly CVarDef<string> CharacterPreviewMode =
        CVarDef.Create("lp.character_preview_mode", "Rotate", CVar.ARCHIVE | CVar.CLIENTONLY);
}
