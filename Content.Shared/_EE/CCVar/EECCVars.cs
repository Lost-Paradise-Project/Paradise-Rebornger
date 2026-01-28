using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._EE.CCVar;

[CVarDefs]
public sealed partial class EECCVars : CVars
{

    #region Contests System

    /// <summary>
    ///     The MASTER TOGGLE for the entire Contests System.
    ///     ALL CONTESTS BELOW, regardless of type or setting will output 1f when false.
    /// </summary>
    public static readonly CVarDef<bool> DoContestsSystem =
        CVarDef.Create("contests.do_contests_system", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all MassContest functions. All mass contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoMassContests =
        CVarDef.Create("contests.do_mass_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     Toggles all StaminaContest functions. All stamina contests output 1f when false
    /// </summary>
    public static readonly CVarDef<bool> DoStaminaContests =
        CVarDef.Create("contests.do_stamina_contests", true, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     The maximum amount that Contests can modify a physics multiplier, given as a +/- percentage
    ///     Default of 0.25f outputs between * 0.75f and 1.25f
    /// </summary>
    public static readonly CVarDef<float> ContestsMaxPercentage =
        CVarDef.Create("contests.max_percentage", 0.25f, CVar.REPLICATED | CVar.SERVER);

    // FRONTIER EDITS:
    /// <summary>
    /// base throwing speed reduction
    /// </summary>
    public static readonly CVarDef<float> BaseDistanceCoeff =
        CVarDef.Create("contests.base_distance_coeff", 0.5f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// max throwing speed reduction
    /// </summary>
    public static readonly CVarDef<float> MaxDistanceCoeff =
        CVarDef.Create("contests.max_distance_coeff", 1.0f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// max throw distance
    /// </summary>
    public static readonly CVarDef<float> DefaultMaxThrowDistance =
        CVarDef.Create("contests.default_max_throw_distance", 4.0f, CVar.REPLICATED | CVar.SERVER);

    #endregion
}
