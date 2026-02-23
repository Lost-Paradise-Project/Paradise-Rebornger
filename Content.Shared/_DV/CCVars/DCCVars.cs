using Robust.Shared.Configuration;

namespace Content.Shared._DV.CCVars;

/// <summary>
/// DeltaV specific cvars.
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming - Shush you
public sealed partial class DCCVars
{
    /// <summary>
    ///     Use the admin's Admin OOC color in bwoinks.
    ///     If either the ooc color or this is not set, uses the admin.admin_bwoink_color value.
    /// </summary>
    public static readonly CVarDef<bool> UseAdminOOCColorInBwoinks =
        CVarDef.Create("admin.bwoink_use_admin_ooc_color", false, CVar.SERVERONLY);

    /// <summary>
    ///     If an admin replies to users from discord, should it use their discord role color? (if applicable)
    ///     Overrides DiscordReplyColor and AdminBwoinkColor.
    /// </summary>
    public static readonly CVarDef<bool> UseDiscordRoleColor =
        CVarDef.Create("admin.use_discord_role_color", false, CVar.SERVERONLY);

    /// <summary>
    ///     If an admin replies to users from discord, should it use their discord role name? (if applicable)
    /// </summary>
    public static readonly CVarDef<bool> UseDiscordRoleName =
        CVarDef.Create("admin.use_discord_role_name", false, CVar.SERVERONLY);

    /// <summary>
    ///     The text before an admin's name when replying from discord to indicate they're speaking from discord.
    /// </summary>
    public static readonly CVarDef<string> DiscordReplyPrefix =
        CVarDef.Create("admin.discord_reply_prefix", "(DC) ", CVar.SERVERONLY);

    /// <summary>
    ///     The color of the names of admins. This is the fallback color for admins.
    /// </summary>
    public static readonly CVarDef<string> AdminBwoinkColor =
        CVarDef.Create("admin.admin_bwoink_color", "red", CVar.SERVERONLY);

    /// <summary>
    ///     The color of the names of admins who reply from discord. Leave empty to disable.
    ///     Overrides AdminBwoinkColor.
    /// </summary>
    public static readonly CVarDef<string> DiscordReplyColor =
        CVarDef.Create("admin.discord_reply_color", string.Empty, CVar.SERVERONLY);
}
