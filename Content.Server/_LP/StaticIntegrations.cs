using Robust.Shared.Network;
using Content.Shared.Humanoid;
using Content.Shared._LP;
using Robust.Shared.Player;
using Content.Shared.Humanoid.Markings;
using System.Linq;
using Robust.Shared.Configuration;

namespace Content.Server._LP.Sponsors;

public static class SponsorSimpleManager
{
    private static IConfigurationManager _cfg => IoCManager.Resolve<IConfigurationManager>();
#if LP
    private static SponsorsManager manager => IoCManager.Resolve<SponsorsManager>();
#endif

    public static int GetTier(NetUserId netId)
    {
        int sponsorTier = _cfg.GetCVar(LPCvars.SponsorLevelHack);
#if LP
        if (manager.TryGetInfo(netId, out var sponsorInfo))
            sponsorTier = sponsorInfo.Tier;
#endif
        return sponsorTier;
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
        List<string> AllowedMarkings = new();
        int sponsorTier = _cfg.GetCVar(LPCvars.SponsorLevelHack);   //возможно потом добавить контроль за исполнением
#if LP
        if (manager.TryGetInfo(netId, out var sponsorInfo))
        {
            sponsorTier = sponsorInfo.Tier;
            AllowedMarkings.AddRange(sponsorInfo.AllowedMarkings.AsEnumerable());
        }
#endif
        if (sponsorTier >= 3)
        {
            foreach (var layer in Enum.GetValues<HumanoidVisualLayers>())
            {
                var sponsormarks = IoCManager.Resolve<MarkingManager>().MarkingsByLayer(layer).Select((a, _) => a.Value).Where(a => a.SponsorOnly == true).Select((a, _) => a.ID).ToList();
                sponsormarks.AddRange(AllowedMarkings);
                marks.AddRange(sponsormarks);
            }
        }

        return marks;
    }

    public static int GetMaxCharacterSlots(NetUserId netId)
    {
        var tier = GetTier(netId);
        return 5 * tier;    // за каждый уровень + 5 слотов
    }

}
