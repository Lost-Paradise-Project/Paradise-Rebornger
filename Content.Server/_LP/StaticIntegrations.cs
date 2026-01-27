using Robust.Shared.Network;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Humanoid.Markings;
using System.Linq;

namespace Content.Server._LP.Sponsors;

public static class SponsorSimpleManager
{
#if LP
    private static SponsorsManager manager => IoCManager.Resolve<SponsorsManager>();
#endif
    public static int GetTier(NetUserId netId)
    {
#if LP
        if (manager.TryGetInfo(netId, out var sponsorInfo))
            return sponsorInfo.Tier;
#endif
        return 0;
    }

    public static int GetTier(EntityUid uid)
    {
        if (IoCManager.Resolve<EntityManager>().TryGetComponent(uid, out ActorComponent? mind) && mind.PlayerSession.UserId is NetUserId userId)
        {
            return GetTier(userId);
        }

        return 0;
    }

    public static string GetUUID(EntityUid uid)
    {
        if (IoCManager.Resolve<EntityManager>().TryGetComponent(uid, out ActorComponent? mind) && mind.PlayerSession.UserId is NetUserId userId)
        {
            return userId.ToString();
        }

        return string.Empty;
    }

    public static string GetUUID(NetUserId netId)
    {
#if LP
        if (manager.TryGetInfo(netId, out var sponsorInfo))
            return sponsorInfo.UUID;
#endif
        return netId.ToString();
    }

    public static List<string> GetMarkings(NetUserId netId)
    {
        List<string> marks = new();
#if LP
        if (manager.TryGetInfo(netId, out var sponsorInfo))
        {
            var sponsorTier = sponsorInfo.Tier;
            if (sponsorTier >= 3)
            {
                var sponsormarks = IoCManager.Resolve<MarkingManager>().Markings.Select((a, _) => a.Value).Where(a => a.SponsorOnly == true).Select((a, _) => a.ID).ToList();
                sponsormarks.AddRange(sponsorInfo.AllowedMarkings.AsEnumerable());
                marks.AddRange(sponsormarks);
            }
        }
#endif
        return marks;
    }

    public static int GetMaxCharacterSlots(NetUserId netId)
    {
        var tier = GetTier(netId);
        return 5 * tier;    // за каждый уровень + 5 слотов
    }

}
