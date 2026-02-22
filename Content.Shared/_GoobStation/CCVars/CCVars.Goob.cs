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

    /// <summary>
    /// Set to true to enable voice barks and disable default speech sounds.
    /// </summary>
    public static readonly CVarDef<bool> BarksEnabled =
        CVarDef.Create("voice.barks_enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Client volume setting for barks.
    /// </summary>
    public static readonly CVarDef<float> BarksVolume =
        CVarDef.Create("voice.barks_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

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

    /// <summary>
    /// Whether or not to spawn space whales if the entity is too far away from the station
    /// </summary>
    public static readonly CVarDef<bool> SpaceWhaleSpawn =
        CVarDef.Create("misc.space_whale_spawn", true, CVar.SERVER);

    /// <summary>
    /// The distance to spawn a space whale from the station
    /// </summary>
    public static readonly CVarDef<int> SpaceWhaleSpawnDistance =
        CVarDef.Create("misc.space_whale_spawn_distance", 1965, CVar.SERVER);
}
