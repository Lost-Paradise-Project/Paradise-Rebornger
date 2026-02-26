using Robust.Shared.Configuration;

namespace Content.Shared._GoobStation.Common.CCVar;

[CVarDefs]
public sealed partial class GoobCVars
{
    /// <summary>
    ///     Discord Webhook for the station report
    /// </summary>
    public static readonly CVarDef<string> StationReportDiscordWebHook =
        CVarDef.Create("stationreport.discord_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
