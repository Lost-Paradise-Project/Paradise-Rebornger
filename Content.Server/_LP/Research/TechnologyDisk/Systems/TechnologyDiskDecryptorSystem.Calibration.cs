using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 1: a marker bounces back and forth along [0,1], lock it in while it's over the target zone.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private static float CalibrationPeriodForTier(int tier) => tier switch { 1 => 1.4f, 2 => 1.05f, _ => 0.8f };
    private static float CalibrationZoneWidthForTier(int tier) => tier switch { 1 => 0.24f, 2 => 0.18f, _ => 0.12f };

    private void StartCalibration(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var tier = ent.Comp.DiskTier ?? 1;
        var period = Math.Max(CalibrationPeriodForTier(tier) - ent.Comp.CurrentLayer * 0.15f, 0.5f);
        var zoneWidth = Math.Max(CalibrationZoneWidthForTier(tier) - ent.Comp.CurrentLayer * 0.03f, 0.08f);

        ent.Comp.CalibrationPeriodSeconds = period;
        ent.Comp.CalibrationZoneWidth = zoneWidth;
        ent.Comp.CalibrationZoneStart = _random.NextFloat(0f, 1f - zoneWidth);
        ent.Comp.CalibrationStartTime = _timing.CurTime;

        BeginTimer(ent, period * 8f); // a few full back-and-forth passes to line up a lock
    }

    private static float GetCalibrationMarkerPosition(TechnologyDiskDecryptorComponent comp, TimeSpan now)
    {
        if (comp.CalibrationPeriodSeconds <= 0f)
            return 0f;

        var cycle = comp.CalibrationPeriodSeconds * 2f;
        var t = (float)(now - comp.CalibrationStartTime).TotalSeconds % cycle;
        if (t < 0f)
            t += cycle;

        return t < comp.CalibrationPeriodSeconds ? t / comp.CalibrationPeriodSeconds : 2f - t / comp.CalibrationPeriodSeconds;
    }

    private void OnLockCalibration(Entity<TechnologyDiskDecryptorComponent> ent, ref DiskDecryptorLockCalibrationMessage args)
    {
        if (ent.Comp.Phase != DecryptorPhase.Manual || ent.Comp.MinigameKind != MinigameKind.Calibration)
            return;

        if (WasMarkerInZoneRecently(ent.Comp, _timing.CurTime))
        {
            _audio.PlayPvs(ent.Comp.NodeSound, ent);
            CompleteLayer(ent);
        }
        else
        {
            ApplyMistake(ent);
        }
    }

    // checking only the exact click moment is too strict, by the time the message
    // arrives the marker's often already moved past the zone. Sample a short window
    // instead of one instant, to give latency some room
    private static bool WasMarkerInZoneRecently(TechnologyDiskDecryptorComponent comp, TimeSpan now)
    {
        const float toleranceSeconds = 0.2f;
        const int samples = 4;

        for (var i = 0; i <= samples; i++)
        {
            var sampleTime = now - TimeSpan.FromSeconds(toleranceSeconds * i / samples);
            var position = GetCalibrationMarkerPosition(comp, sampleTime);
            if (position >= comp.CalibrationZoneStart && position <= comp.CalibrationZoneStart + comp.CalibrationZoneWidth)
                return true;
        }

        return false;
    }
}
