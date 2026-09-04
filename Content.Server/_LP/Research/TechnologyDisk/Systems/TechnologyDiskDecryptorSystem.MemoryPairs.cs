using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 1: classic card-matching.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private static int MemoryPairCountForTier(int tier, int layer) => Math.Clamp(3 + tier + layer, 4, 7);

    private void StartMemoryPairs(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var pairCount = MemoryPairCountForTier(ent.Comp.DiskTier ?? 1, ent.Comp.CurrentLayer);
        var symbols = Enumerable.Range(0, pairCount).SelectMany(i => new[] { i, i }).ToList();
        _random.Shuffle(symbols);

        ent.Comp.MemorySymbols = symbols;
        ent.Comp.MemoryMatched = Enumerable.Repeat(false, symbols.Count).ToList();
        ent.Comp.MemorySelected.Clear();
        BeginTimer(ent, 10f + pairCount * 6f);
    }

    private void HandleMemoryClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        var symbols = ent.Comp.MemorySymbols;
        var matched = ent.Comp.MemoryMatched;
        var selected = ent.Comp.MemorySelected;

        if (index < 0 || index >= symbols.Count)
            return;

        // a mismatched pair stays face up so the player can read it - clicking anywhere
        // else is what flips it back down and starts the next attempt
        if (selected.Count == 2)
            selected.Clear();

        if (matched[index] || selected.Contains(index))
            return;

        selected.Add(index);

        if (selected.Count == 2)
        {
            _audio.PlayPvs(ent.Comp.NodeSound, ent);

            if (symbols[selected[0]] == symbols[selected[1]])
            {
                matched[selected[0]] = matched[selected[1]] = true;
                selected.Clear();

                if (matched.All(m => m))
                {
                    CompleteLayer(ent);
                    return;
                }
            }
        }

        UpdateUi(ent);
    }
}
