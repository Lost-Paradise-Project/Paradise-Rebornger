using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 1: a rotate-the-pipes hack. A path of segments is laid across the grid and each
// one starts scrambled to the wrong rotation - click a segment to turn it 90°, goal is
// to line every segment's connectors up with its neighbours so the flow runs unbroken
// from start to end.
public sealed partial class TechnologyDiskDecryptorSystem
{
    // bit0=North, bit1=East, bit2=South, bit3=West
    private const int North = 1;
    private const int East = 2;
    private const int South = 4;
    private const int West = 8;

    private void StartPipe(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var layer = ent.Comp.CurrentLayer;
        var size = Math.Clamp(5 + layer, 5, 7);
        var pathLength = 8 + layer * 3;
        var path = GenerateSelfAvoidingWalk(size, pathLength);
        var total = size * size;
        var baseMask = new int[total];

        for (var i = 0; i < path.Count; i++)
        {
            var mask = 0;
            if (i > 0)
                mask |= DirectionTo(path[i], path[i - 1], size);
            if (i < path.Count - 1)
                mask |= DirectionTo(path[i], path[i + 1], size);

            baseMask[path[i]] = mask;
        }

        // on the last layer, never let a segment be a single click away from
        // solved - forces at least two turns on every piece instead of letting
        // the player get lucky
        var minScramble = layer >= ent.Comp.TotalLayers - 1 ? 2 : 1;

        var rotation = new int[total];
        foreach (var cell in path)
        {
            int r;
            do
            {
                r = _random.Next(4);
            } while (RotateCw(baseMask[cell], r) == baseMask[cell] || r < minScramble);

            rotation[cell] = r;
        }

        ent.Comp.PipeSize = size;
        ent.Comp.PipeBaseMask = baseMask.ToList();
        ent.Comp.PipeRotation = rotation.ToList();
        ent.Comp.PipeStartIndex = path[0];
        ent.Comp.PipeEndIndex = path[^1];

        BeginTimer(ent, 18f + size * 3.5f);
    }

    /// <summary>Which single-step direction, as a bitmask, points from <paramref name="from"/> to an orthogonally adjacent <paramref name="to"/>.</summary>
    private static int DirectionTo(int from, int to, int size)
    {
        if (to == from - size) return North;
        if (to == from + size) return South;
        if (to == from + 1) return East;
        if (to == from - 1) return West;
        return 0;
    }

    /// <summary>Rotates a N/E/S/W bitmask 90° clockwise, <paramref name="steps"/> times.</summary>
    private static int RotateCw(int mask, int steps)
    {
        for (var i = 0; i < steps; i++)
        {
            mask = ((mask << 1) | (mask >> 3)) & 0b1111;
        }

        return mask;
    }

    /// <summary>Plain random self-avoiding walk (no special traversal rule like Breach or Circuit - any orthogonal neighbour is fine), retrying from scratch if it paints itself into a corner.</summary>
    private List<int> GenerateSelfAvoidingWalk(int size, int length)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var path = new List<int> { _random.Next(size * size) };
            var visited = new HashSet<int> { path[0] };

            while (path.Count < length)
            {
                var options = GetNeighbours(path[^1], size).Where(n => !visited.Contains(n)).ToList();
                if (options.Count == 0)
                    break;

                var next = options[_random.Next(options.Count)];
                path.Add(next);
                visited.Add(next);
            }

            if (path.Count == length)
                return path;
        }

        return GenerateSelfAvoidingWalk(size, Math.Max(2, length - 1));
    }

    private static bool IsPipeSolved(TechnologyDiskDecryptorComponent comp)
    {
        for (var i = 0; i < comp.PipeBaseMask.Count; i++)
        {
            var baseMask = comp.PipeBaseMask[i];
            if (baseMask == 0)
                continue; // not a pipe cell, nothing to check

            if (RotateCw(baseMask, comp.PipeRotation[i]) != baseMask)
                return false;
        }

        return true;
    }

    private void HandlePipeClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        if (index < 0 || index >= ent.Comp.PipeBaseMask.Count || ent.Comp.PipeBaseMask[index] == 0)
            return; // clicking a blank (non-pipe) cell does nothing - not a mistake, just inert

        ent.Comp.PipeRotation[index] = (ent.Comp.PipeRotation[index] + 1) % 4;
        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        if (IsPipeSolved(ent.Comp))
        {
            CompleteLayer(ent);
            return;
        }

        UpdateUi(ent);
    }
}
