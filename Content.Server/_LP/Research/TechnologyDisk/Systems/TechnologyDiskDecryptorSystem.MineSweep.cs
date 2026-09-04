using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 3: minesweeper. Reveal every safe cell without ever clicking a trap. Revealed
// safe cells show how many traps around them, a 0 auto-reveals its neighbours the same
// way. Hitting a trap regenerates a whole new board and costs integrity - same weight
// as any other tier 3 mistake.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private void StartMineSweep(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var layer = ent.Comp.CurrentLayer;
        var size = Math.Clamp(4 + layer, 4, 6);
        var total = size * size;
        var mineCount = Math.Clamp(4 + layer * 3, 4, total / 2);

        var mines = new bool[total];
        foreach (var i in Enumerable.Range(0, total).OrderBy(_ => _random.Next()).Take(mineCount))
        {
            mines[i] = true;
        }

        ent.Comp.SweepSize = size;
        ent.Comp.SweepMines = mines.ToList();
        ent.Comp.SweepRevealed = Enumerable.Repeat(false, total).ToList();
        ent.Comp.SweepFirstClickDone = false;

        BeginTimer(ent, 20f + size * 4f);
    }

    private static int CountAdjacentMines(List<bool> mines, int index, int size)
    {
        var count = 0;
        foreach (var neighbour in Get8Neighbours(index, size))
        {
            if (mines[neighbour])
                count++;
        }

        return count;
    }

    private static IEnumerable<int> Get8Neighbours(int index, int size)
    {
        var x = index % size;
        var y = index / size;

        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || nx >= size || ny < 0 || ny >= size)
                    continue;

                yield return ny * size + nx;
            }
        }
    }

    // re-rolls traps so the safe cell and its 8 neighbours are guaranteed clear -
    // the first click always opens a real patch, not just a lone number
    private void MakeSweepOpeningSafe(Entity<TechnologyDiskDecryptorComponent> ent, int safeIndex)
    {
        var size = ent.Comp.SweepSize;
        var total = size * size;
        var mineCount = ent.Comp.SweepMines.Count(m => m);

        var excluded = new HashSet<int> { safeIndex };
        foreach (var n in Get8Neighbours(safeIndex, size))
            excluded.Add(n);

        var candidates = Enumerable.Range(0, total)
            .Where(i => !excluded.Contains(i))
            .OrderBy(_ => _random.Next())
            .Take(Math.Min(mineCount, total - excluded.Count))
            .ToList();

        var mines = new bool[total];
        foreach (var i in candidates)
            mines[i] = true;

        ent.Comp.SweepMines = mines.ToList();
    }

    private void HandleMineSweepClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        var revealed = ent.Comp.SweepRevealed;
        var size = ent.Comp.SweepSize;

        if (index < 0 || index >= ent.Comp.SweepMines.Count || revealed[index])
            return;

        if (!ent.Comp.SweepFirstClickDone)
        {
            ent.Comp.SweepFirstClickDone = true;
            MakeSweepOpeningSafe(ent, index);
        }

        var mines = ent.Comp.SweepMines;

        if (mines[index])
        {
            revealed[index] = true;
            ApplyMistake(ent);
            return;
        }

        // flood-reveal from here - any connected run of "0 adjacent mines" cells
        // opens up along with the ring of numbered cells around it
        var queue = new Queue<int>();
        queue.Enqueue(index);
        revealed[index] = true;

        while (queue.TryDequeue(out var current))
        {
            if (CountAdjacentMines(mines, current, size) != 0)
                continue;

            foreach (var neighbour in GetNeighbours(current, size))
            {
                if (revealed[neighbour] || mines[neighbour])
                    continue;

                revealed[neighbour] = true;
                queue.Enqueue(neighbour);
            }
        }

        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        var allSafeRevealed = true;
        for (var i = 0; i < mines.Count; i++)
        {
            if (!mines[i] && !revealed[i])
            {
                allSafeRevealed = false;
                break;
            }
        }

        if (allSafeRevealed)
        {
            CompleteLayer(ent);
            return;
        }

        UpdateUi(ent);
    }
}
