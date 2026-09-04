using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 2: a single cell lights up at a time - click it before it goes dark. A miss
// (timeout) or a wrong click both count as a real mistake, but only respawn the current
// target instead of wiping overall hit progress.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private void StartSignalJam(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var layer = ent.Comp.CurrentLayer;
        var size = Math.Clamp(3 + layer, 3, 5);

        ent.Comp.JamSize = size;
        ent.Comp.JamHitsNeeded = Math.Clamp(6 + layer * 2, 6, 12);
        ent.Comp.JamHitsDone = 0;

        SpawnJamTarget(ent);
        BeginTimer(ent, 6f + ent.Comp.JamHitsNeeded * 2.2f);
    }

    private void SpawnJamTarget(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var total = ent.Comp.JamSize * ent.Comp.JamSize;
        ent.Comp.JamActiveCell = _random.Next(total);

        var visibleSeconds = Math.Max(1.4f - ent.Comp.CurrentLayer * 0.15f, 0.7f);
        ent.Comp.JamActiveDeadline = _timing.CurTime + TimeSpan.FromSeconds(visibleSeconds);
    }

    /// <summary>Called every tick while Signal Jam is running - separate from the generic per-attempt timeout, since a missed target here shouldn't fail the whole layer.</summary>
    private void TickSignalJam(Entity<TechnologyDiskDecryptorComponent> ent, TimeSpan now)
    {
        if (now >= ent.Comp.JamActiveDeadline)
            ApplyMistake(ent);
    }

    private void HandleSignalJamClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        if (index != ent.Comp.JamActiveCell)
        {
            ApplyMistake(ent);
            return;
        }

        ent.Comp.JamHitsDone++;
        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        if (ent.Comp.JamHitsDone >= ent.Comp.JamHitsNeeded)
        {
            CompleteLayer(ent);
            return;
        }

        SpawnJamTarget(ent);
        UpdateUi(ent);
    }
}
