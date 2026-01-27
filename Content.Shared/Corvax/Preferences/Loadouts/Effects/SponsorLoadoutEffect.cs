using System.Diagnostics.CodeAnalysis;
using Content.Corvax.Interfaces.Shared;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Only sponsor that have it can select this loadout
/// </summary>
public sealed partial class SponsorLoadoutEffect : LoadoutEffect
{
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

        if (sponsorTier < 3)    //LP edit - любые лодауты спонсорам 3+ уровня
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-sponsor-only"));
            return false;
        }

        return true;
    }
}
