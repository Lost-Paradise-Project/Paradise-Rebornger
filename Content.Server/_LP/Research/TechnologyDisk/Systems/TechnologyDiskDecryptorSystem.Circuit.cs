using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 3: trace a path across a proper maze from the start room to the end room.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private void StartCircuit(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        // "rooms" is junctions per side - the displayed grid is twice that minus
        // one, since every other row/column is corridor rather than a room itself.
        var rooms = Math.Clamp(3 + ent.Comp.CurrentLayer, 3, 5);
        var (size, blocked, start, end) = GenerateMaze(rooms, ent.Comp.CurrentLayer);

        ent.Comp.CircuitSize = size;
        ent.Comp.CircuitBlocked = blocked;
        ent.Comp.CircuitStartIndex = start;
        ent.Comp.CircuitEndIndex = end;
        ent.Comp.CircuitPath = new List<int> { start };

        // enough slack to explore a couple of wrong turns and step back out of them, not enough to wander forever
        ent.Comp.CircuitMovesBudget = ShortestPathLength(blocked, size, start, end) + rooms * 2;
        ent.Comp.CircuitMovesLeft = ent.Comp.CircuitMovesBudget;

        BeginTimer(ent, 15f + size * 2.5f);
    }

    // carves corridors between "rooms" on the even grid coordinates, the odd cells in
    // between only open up once a passage is carved, giving real dead ends instead of
    // just scattering random blocked cells
    private (int size, List<bool> blocked, int start, int end) GenerateMaze(int rooms, int layer)
    {
        var size = rooms * 2 - 1;
        var blocked = Enumerable.Repeat(true, size * size).ToList();
        var visited = new bool[rooms, rooms];

        void Carve(int rx, int ry)
        {
            visited[rx, ry] = true;
            blocked[ry * 2 * size + rx * 2] = false;

            var dirs = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            _random.Shuffle(dirs);

            foreach (var (dx, dy) in dirs)
            {
                var nx = rx + dx;
                var ny = ry + dy;
                if (nx < 0 || nx >= rooms || ny < 0 || ny >= rooms || visited[nx, ny])
                    continue;

                blocked[(ry * 2 + dy) * size + (rx * 2 + dx)] = false;
                Carve(nx, ny);
            }
        }

        Carve(0, 0);

        // a strict spanning tree has exactly one route through, which reads more like
        // guessing than solving, punch a few extra holes for the occasional shortcut,
        // fewer of them on later layers where we actually want the maze to bite
        var wallCells = Enumerable.Range(0, blocked.Count).Where(i => blocked[i]).ToList();
        _random.Shuffle(wallCells);
        foreach (var index in wallCells.Take(Math.Max(0, rooms - layer)))
        {
            blocked[index] = false;
        }

        return (size, blocked, 0, (rooms - 1) * 2 * size + (rooms - 1) * 2);
    }

    private static int ShortestPathLength(List<bool> blocked, int size, int start, int end)
    {
        var dist = new int[blocked.Count];
        Array.Fill(dist, -1);
        dist[start] = 0;

        var queue = new Queue<int>();
        queue.Enqueue(start);

        while (queue.TryDequeue(out var current))
        {
            if (current == end)
                break;

            foreach (var next in GetNeighbours(current, size))
            {
                if (!blocked[next] && dist[next] == -1)
                {
                    dist[next] = dist[current] + 1;
                    queue.Enqueue(next);
                }
            }
        }

        return Math.Max(0, dist[end]);
    }

    private static IEnumerable<int> GetNeighbours(int index, int size)
    {
        var x = index % size;
        var y = index / size;

        if (x > 0) yield return index - 1;
        if (x < size - 1) yield return index + 1;
        if (y > 0) yield return index - size;
        if (y < size - 1) yield return index + size;
    }

    private static bool AreAdjacent(int a, int b, int size) =>
        Math.Abs(a % size - b % size) + Math.Abs(a / size - b / size) == 1;

    private void HandleCircuitClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        var path = ent.Comp.CircuitPath;
        if (index < 0 || index >= ent.Comp.CircuitSize * ent.Comp.CircuitSize || path.Count == 0)
            return;

        // stepping back onto the cell you just came from is a deliberate retreat, not a
        // mistake, otherwise wandering into any dead end would be an instant, unavoidable fail
        if (path.Count >= 2 && index == path[^2])
        {
            path.RemoveAt(path.Count - 1);
            ent.Comp.CircuitMovesLeft--;
        }
        else if (ent.Comp.CircuitBlocked[index] || path.Contains(index) || !AreAdjacent(path[^1], index, ent.Comp.CircuitSize))
        {
            ApplyMistake(ent);
            return;
        }
        else
        {
            path.Add(index);
            ent.Comp.CircuitMovesLeft--;
            _audio.PlayPvs(ent.Comp.NodeSound, ent);

            if (index == ent.Comp.CircuitEndIndex)
            {
                CompleteLayer(ent);
                return;
            }
        }

        if (ent.Comp.CircuitMovesLeft <= 0)
            RetryCircuit(ent); // ran the maze out of slack, not an actual wrong move - no integrity cost
        else
            UpdateUi(ent);
    }

    // Resets the walked path and refills the moves budget on the same maze, without
    // touching integrity. Used when the player runs out of moves through honest exploration.
    private void RetryCircuit(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        _audio.PlayPvs(ent.Comp.MistakeSound, ent);
        ent.Comp.CircuitPath = new List<int> { ent.Comp.CircuitStartIndex };
        ent.Comp.CircuitMovesLeft = ent.Comp.CircuitMovesBudget;
        UpdateUi(ent);
    }
}
