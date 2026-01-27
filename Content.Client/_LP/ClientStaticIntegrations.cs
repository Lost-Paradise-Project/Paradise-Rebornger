using System.Linq;
using Content.Shared.Humanoid.Markings;

namespace Content.Client._LP.Sponsors;

/// <summary>
/// Класс-упрощение для того, чтобы не плодить жуткие строки кода
/// </summary>
public static class SponsorSimpleManager
{
#if LP
    private static SponsorsManager manager => IoCManager.Resolve<SponsorsManager>();
#endif
    public static int GetTier()
    {
#if LP
        if (manager.TryGetInfo(out var sponsorInfo))
            return sponsorInfo.Tier;
#endif
        return 0;
    }

    public static string GetUUID()
    {
#if LP
        if (manager.TryGetInfo(out var sponsorInfo))
            return sponsorInfo.UUID;   //Здесь хранится NetUserId, а не имя. опасно менять из-за json
#endif
        return "";
    }

    public static List<string> GetMarkings()
    {
        List<string> marks = new();
#if LP
        if (manager.TryGetInfo(out var sponsorInfo))
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

    public static int GetMaxCharacterSlots()
    {
        var tier = GetTier();
        return 5 * tier;    // за каждый уровень + 5 слотов
    }
}
