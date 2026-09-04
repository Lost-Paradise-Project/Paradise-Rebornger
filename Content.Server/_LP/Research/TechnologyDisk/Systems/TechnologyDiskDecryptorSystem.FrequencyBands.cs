using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 2: like Calibration, but several markers bounce at once and each has to be
// locked individually (click index = which bar). A miss only re-rolls that one bar's
// zone/timing, it doesn't touch the others' progress.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private void StartFrequencyBands(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var layer = ent.Comp.CurrentLayer;
        var count = Math.Clamp(2 + layer, 2, 3);

        ent.Comp.BandsPeriod = new List<float>(count);
        ent.Comp.BandsZoneStart = new List<float>(count);
        ent.Comp.BandsZoneWidth = new List<float>(count);
        ent.Comp.BandsStartTime = new List<TimeSpan>(count);
        ent.Comp.BandsSolved = Enumerable.Repeat(false, count).ToList();

        for (var i = 0; i < count; i++)
        {
            RollBand(ent, i);
        }

        BeginTimer(ent, 22f + count * 6f);
    }

    private void RollBand(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        var period = Math.Max(1.3f - index * 0.15f - ent.Comp.CurrentLayer * 0.1f, 0.5f);
        var zoneWidth = Math.Max(0.22f - ent.Comp.CurrentLayer * 0.02f, 0.1f);

        if (ent.Comp.BandsPeriod.Count <= index)
        {
            ent.Comp.BandsPeriod.Add(period);
            ent.Comp.BandsZoneStart.Add(0f);
            ent.Comp.BandsZoneWidth.Add(zoneWidth);
            ent.Comp.BandsStartTime.Add(_timing.CurTime);
        }
        else
        {
            ent.Comp.BandsPeriod[index] = period;
            ent.Comp.BandsZoneWidth[index] = zoneWidth;
            ent.Comp.BandsStartTime[index] = _timing.CurTime;
        }

        ent.Comp.BandsZoneStart[index] = _random.NextFloat(0f, 1f - zoneWidth);
    }

    private static float GetBandPosition(float period, TimeSpan startTime, TimeSpan now)
    {
        if (period <= 0f)
            return 0f;

        var cycle = period * 2f;
        var t = (float)(now - startTime).TotalSeconds % cycle;
        if (t < 0f)
            t += cycle;

        return t < period ? t / period : 2f - t / period;
    }

    private void HandleFrequencyBandsClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        if (index < 0 || index >= ent.Comp.BandsSolved.Count || ent.Comp.BandsSolved[index])
            return;

        var position = GetBandPosition(ent.Comp.BandsPeriod[index], ent.Comp.BandsStartTime[index], _timing.CurTime);
        var inZone = position >= ent.Comp.BandsZoneStart[index] && position <= ent.Comp.BandsZoneStart[index] + ent.Comp.BandsZoneWidth[index];

        if (!inZone)
        {
            ent.Comp.Integrity = Math.Max(0, ent.Comp.Integrity - ent.Comp.IntegrityPenaltyPerMistake);
            _audio.PlayPvs(ent.Comp.MistakeSound, ent);

            if (ent.Comp.Integrity <= 0)
            {
                FailDecryption(ent);
                return;
            }

            RollBand(ent, index);
            UpdateUi(ent);
            return;
        }

        ent.Comp.BandsSolved[index] = true;
        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        if (ent.Comp.BandsSolved.All(solved => solved))
        {
            CompleteLayer(ent);
            return;
        }

        UpdateUi(ent);
    }
}
