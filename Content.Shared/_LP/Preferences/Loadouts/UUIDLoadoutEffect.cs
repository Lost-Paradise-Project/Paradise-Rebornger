using System.Diagnostics.CodeAnalysis;
using Content.Corvax.Interfaces.Shared;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Only player with correct uuid can select this loadout
/// </summary>
public sealed partial class UUIDLoadoutEffect : LoadoutEffect
{
    [DataField("UUID", required: true)]
    public string UUID = default!;
    public override bool Validate(HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        LoadoutPrototype proto, // Corvax-Sponsors
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason,
        int sponsorTier = 0,    //LP edit
        string uuid = ""        //LP edit
    )
    {
        reason = null;

        if (session == null)
            return true;

        if (uuid.ToLower() != UUID.ToLower())
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-uuid-only"));
            return false;
        }

        return true;
    }
}
