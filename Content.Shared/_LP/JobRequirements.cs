using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class UUIDRequirement : JobRequirement
{
    [DataField(required: true)]
    public string uid;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason,
        int sponsorTier = 0,  //LP edit
        string uuid = ""  //LP edit
    )
    {
        reason = new FormattedMessage();

        if (uuid.ToLower() == uid.ToLower())
            return true;

        reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-uuid-only"));
        return false;
    }
}

[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class SponsorTierRequirement : JobRequirement
{
    [DataField]
    public int tier = 3;

    public override bool Check(IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason,
        int sponsorTier = 0,  //LP edit
        string uuid = ""  //LP edit
    )
    {
        reason = new FormattedMessage();

        if (tier <= sponsorTier)
            return true;

        reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-sponsor-only"));
        return false;
    }
}
