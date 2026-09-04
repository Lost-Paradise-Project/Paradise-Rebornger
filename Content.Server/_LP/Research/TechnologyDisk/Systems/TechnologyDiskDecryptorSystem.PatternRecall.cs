using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 1: flash a set of cells briefly, hide them, then reproduce the same set from
// memory (order doesn't matter, just which cells). Clicking during the reveal window
// is simply ignored.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private void StartPatternRecall(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var layer = ent.Comp.CurrentLayer;
        var size = Math.Clamp(3 + layer, 3, 5);
        var total = size * size;
        var targetCount = Math.Clamp(4 + layer * 2, 4, total - 1);

        var targets = new bool[total];
        foreach (var i in Enumerable.Range(0, total).OrderBy(_ => _random.Next()).Take(targetCount))
        {
            targets[i] = true;
        }

        var revealSeconds = Math.Max(2.6f - layer * 0.4f, 1.2f);

        ent.Comp.PatternSize = size;
        ent.Comp.PatternTargets = targets.ToList();
        ent.Comp.PatternFound = Enumerable.Repeat(false, total).ToList();
        ent.Comp.PatternRevealSeconds = revealSeconds;
        ent.Comp.PatternRevealEndTime = _timing.CurTime + TimeSpan.FromSeconds(revealSeconds);

        BeginTimer(ent, revealSeconds + 8f + targetCount * 1.5f);
    }

    private void HandlePatternClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        if (_timing.CurTime < ent.Comp.PatternRevealEndTime)
            return; // still showing the pattern - clicks don't count yet, but aren't a mistake either

        if (index < 0 || index >= ent.Comp.PatternTargets.Count || ent.Comp.PatternFound[index])
            return;

        if (!ent.Comp.PatternTargets[index])
        {
            ApplyMistake(ent);
            return;
        }

        ent.Comp.PatternFound[index] = true;
        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        if (ent.Comp.PatternFound.Zip(ent.Comp.PatternTargets, (found, target) => !target || found).All(ok => ok))
        {
            CompleteLayer(ent);
            return;
        }

        UpdateUi(ent);
    }
}
