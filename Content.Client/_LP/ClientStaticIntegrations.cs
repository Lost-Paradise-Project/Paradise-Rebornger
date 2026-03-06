using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Configuration;
using Content.Shared._LP;

namespace Content.Client._LP.Sponsors;

/// <summary>
/// Класс-упрощение для того, чтобы не плодить жуткие строки кода
/// </summary>
public static class SponsorSimpleManager
{
    private static IConfigurationManager _cfg => IoCManager.Resolve<IConfigurationManager>();
#if LP
    private static SponsorsManager manager => IoCManager.Resolve<SponsorsManager>();
#endif

    public static int GetTier()
    {
        int sponsorTier = _cfg.GetCVar(LPCvars.SponsorLevelHack);
#if LP
        if (manager.TryGetInfo(out var sponsorInfo))
            sponsorTier = sponsorInfo.Tier;
#endif
        return sponsorTier;
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
        List<string> AllowedMarkings = new();
        int sponsorTier = _cfg.GetCVar(LPCvars.SponsorLevelHack);
#if LP
        if (manager.TryGetInfo(out var sponsorInfo))
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

    public static int GetMaxCharacterSlots()
    {
        var tier = GetTier();
        return 5 * tier;    // за каждый уровень + 5 слотов
    }
}
