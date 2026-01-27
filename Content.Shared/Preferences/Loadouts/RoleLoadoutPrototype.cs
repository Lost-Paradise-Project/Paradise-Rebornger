using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Corresponds to a Job / Antag prototype and specifies loadouts
/// </summary>
[Prototype]
public sealed partial class RoleLoadoutPrototype : IPrototype
{
    /*
     * Separate to JobPrototype / AntagPrototype as they are turning into messy god classes.
     */

    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Can the user edit their entity name for this role loadout?
    /// </summary>
    [DataField]
    public bool CanCustomizeName;

    /// <summary>
    /// Should we use a random name for this loadout?
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? NameDataset;

    // Not required so people can set their names.
    /// <summary>
    /// Groups that comprise this role loadout.
    /// </summary>
    // LP edit start
    [DataField("groups")]
    private List<ProtoId<LoadoutGroupPrototype>> _groups = new();

    private bool _initialized;

    /// <summary>
    /// Groups that comprise this role loadout.
    /// </summary>
    public List<ProtoId<LoadoutGroupPrototype>> Groups
    {
        get
        {
            if (!_initialized)
            {
                _initialized = true;
#if LP
                if (!_groups.Contains("SponsorsClothing"))
                {
                    _groups.Add("SponsorsClothing");
                }
#endif
            }
            return _groups;
        }
    }
    // LP edit end

    /// <summary>
    /// How many points are allotted for this role loadout prototype.
    /// </summary>
    [DataField]
    public int? Points;
}
