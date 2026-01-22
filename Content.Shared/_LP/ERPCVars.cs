using Robust.Shared.Configuration;

namespace Content.Shared._ERPModule.Data;

[CVarDefs]
public sealed class ErpCVars
{
    public static readonly CVarDef<bool> EroticPanelEnabled =
        CVarDef.Create("erp.enabled", true, CVar.SERVER| CVar.REPLICATED);

    public static readonly CVarDef<bool> EroticEmoteVisibility =
        CVarDef.Create("erp.emote", true, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);

    public static readonly CVarDef<float> EroticTolerance =
        CVarDef.Create("erp.tolerance", 1f, CVar.CLIENT | CVar.ARCHIVE | CVar.REPLICATED);
}
