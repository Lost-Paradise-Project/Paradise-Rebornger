using Robust.Shared.Configuration;

namespace Content.Shared._GoobStation.CCVar;

[CVarDefs]
public sealed partial class GoobCVars
{
    /// <summary>
    /// Controls how often GPS updates.
    /// </summary>
    public static readonly CVarDef<float> GpsUpdateRate =
        CVarDef.Create("gps.update_rate", 1f, CVar.SERVER | CVar.REPLICATED);

    #region Goobstation - Chat Highlight sounds!
    // Goobstation - Chat Highlight sounds!
    /// <summary>
    ///     Whether to play a sound when a highlighted message is received.
    /// </summary>
    public static readonly CVarDef<bool> ChatHighlightSound =
        CVarDef.Create("chat.highlight_sound", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    ///     Volume of the highlight sound when a highlighted message is received.
    /// </summary>
    public static readonly CVarDef<float> ChatHighlightVolume =
        CVarDef.Create("chat.highlight_volume", 1.0f, CVar.ARCHIVE | CVar.CLIENTONLY);
    // Goobstation - end
    #endregion
}
