using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 2: click a cell to flip it and its neighbours, goal is all lights off.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private static readonly (int dx, int dy)[] Cross = { (0, 0), (-1, 0), (1, 0), (0, -1), (0, 1) };
    private static int LightsSizeForTier(int tier) => tier switch { 1 => 3, 2 => 4, _ => 5 };

    private void StartLightsOut(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var size = Math.Clamp(LightsSizeForTier(ent.Comp.DiskTier ?? 1) + ent.Comp.CurrentLayer, 3, 6);
        var state = new bool[size * size];

        // scrambling by toggling random cells starting from an all-off board guarantees it's solvable
        for (var i = 0; i < size + ent.Comp.CurrentLayer + 2; i++)
        {
            ToggleLights(state, _random.Next(state.Length), size);
        }

        ent.Comp.LightsSize = size;
        ent.Comp.LightsState = state.ToList();
        BeginTimer(ent, 15f + size * 3f);
    }

    // classic Lights Out rule: toggling a cell also toggles its neighbours
    private static void ToggleLights(bool[] state, int index, int size)
    {
        var x = index % size;
        var y = index / size;

        foreach (var (dx, dy) in Cross)
        {
            var nx = x + dx;
            var ny = y + dy;
            if (nx >= 0 && nx < size && ny >= 0 && ny < size)
                state[ny * size + nx] ^= true;
        }
    }

    private void HandleLightsClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        if (index < 0 || index >= ent.Comp.LightsState.Count)
            return;

        var state = ent.Comp.LightsState.ToArray();
        ToggleLights(state, index, ent.Comp.LightsSize);
        ent.Comp.LightsState = state.ToList();
        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        if (state.All(on => !on))
            CompleteLayer(ent);
        else
            UpdateUi(ent);
    }
}
