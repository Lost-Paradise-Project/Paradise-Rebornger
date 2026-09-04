using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 3: the "hack chain" challenge - trace a route across a sparse node network,
// hitting numbered checkpoints in order, before reaching the final target under time
// pressure. Reuses Circuit's maze carving (GenerateMaze) and AreAdjacent, since a
// branching network with real dead ends is exactly what that already builds; the
// difference here is what's rendered (a wireframe of nodes/lines, not a filled grid)
// and that you have to hit waypoints in sequence rather than just reach the exit.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private void StartFlow(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var layer = ent.Comp.CurrentLayer;
        var rooms = Math.Clamp(2 + layer, 2, 4);
        var (size, blocked, start, end) = GenerateMaze(rooms, layer);

        var solution = ShortestPathNodes(blocked, size, start, end);
        var checkpointCount = Math.Clamp(2 + layer, 2, 4);
        var checkpoints = Enumerable.Range(1, checkpointCount)
            .Select(i => solution[i * (solution.Count - 1) / checkpointCount])
            .Distinct()
            .ToList();

        ent.Comp.FlowSize = size;
        ent.Comp.FlowOpen = blocked.Select(b => !b).ToList();
        ent.Comp.FlowStartIndex = start;
        ent.Comp.FlowCheckpoints = checkpoints;
        ent.Comp.FlowNextCheckpoint = 0;
        ent.Comp.FlowPath = new List<int> { start };

        ent.Comp.FlowMovesBudget = solution.Count - 1 + rooms * 2;
        ent.Comp.FlowMovesLeft = ent.Comp.FlowMovesBudget;

        BeginTimer(ent, 18f + size * 3f);
    }

    /// <summary>BFS shortest route from start to end, as an ordered list of node indices (unlike Circuit's ShortestPathLength, which only needs the distance).</summary>
    private static List<int> ShortestPathNodes(List<bool> blocked, int size, int start, int end)
    {
        var parent = new int[blocked.Count];
        Array.Fill(parent, -1);
        var visited = new bool[blocked.Count];
        visited[start] = true;

        var queue = new Queue<int>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var current))
        {
            if (current == end)
                break;

            foreach (var next in GetNeighbours(current, size))
            {
                if (blocked[next] || visited[next])
                    continue;

                visited[next] = true;
                parent[next] = current;
                queue.Enqueue(next);
            }
        }

        var path = new List<int>();
        for (var node = end; node != -1; node = parent[node])
        {
            path.Add(node);
        }

        path.Reverse();
        return path;
    }

    private void HandleFlowClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        var path = ent.Comp.FlowPath;
        var total = ent.Comp.FlowSize * ent.Comp.FlowSize;
        if (index < 0 || index >= total || path.Count == 0)
            return;

        // stepping back onto the cell you just came from is a deliberate retreat,
        // not a mistake - same reasoning as Circuit
        if (path.Count >= 2 && index == path[^2])
        {
            path.RemoveAt(path.Count - 1);
            ent.Comp.FlowMovesLeft--;
            AfterFlowMove(ent);
            return;
        }

        if (!ent.Comp.FlowOpen[index] || path.Contains(index) || !AreAdjacent(path[^1], index, ent.Comp.FlowSize))
        {
            ApplyMistake(ent);
            return;
        }

        path.Add(index);
        ent.Comp.FlowMovesLeft--;
        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        // checkpoints only advance if hit in order - landing on a later one early
        // just does nothing, it isn't a penalty, you just haven't "activated" it yet
        if (ent.Comp.FlowNextCheckpoint < ent.Comp.FlowCheckpoints.Count &&
            index == ent.Comp.FlowCheckpoints[ent.Comp.FlowNextCheckpoint])
        {
            ent.Comp.FlowNextCheckpoint++;
            if (ent.Comp.FlowNextCheckpoint >= ent.Comp.FlowCheckpoints.Count)
            {
                CompleteLayer(ent);
                return;
            }
        }

        AfterFlowMove(ent);
    }

    private void AfterFlowMove(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        if (ent.Comp.FlowMovesLeft <= 0)
            RetryFlow(ent);
        else
            UpdateUi(ent);
    }

    private void RetryFlow(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        _audio.PlayPvs(ent.Comp.MistakeSound, ent);
        ent.Comp.FlowPath = new List<int> { ent.Comp.FlowStartIndex };
        ent.Comp.FlowNextCheckpoint = 0;
        ent.Comp.FlowMovesLeft = ent.Comp.FlowMovesBudget;
        UpdateUi(ent);
    }
}
