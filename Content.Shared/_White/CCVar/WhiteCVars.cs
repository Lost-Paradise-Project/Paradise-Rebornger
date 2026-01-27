using Robust.Shared.Configuration;
namespace Content.Shared._White.CCVar;

[CVarDefs]
public sealed partial class WhiteCVars
{
    public static readonly CVarDef<bool> CustomGhosts =
    CVarDef.Create("white.custom_ghosts", true, CVar.CLIENT | CVar.ARCHIVE);
}
