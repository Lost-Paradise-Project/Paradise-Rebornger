using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;
using Robust.Shared.Random;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 3: a Cyberpunk style "breach" hack. Pick cells on a grid of short
// codes, alternating between "same column as the last pick" and "same row as the
// last pick" (the very first pick can be anywhere in the top row). Win once the
// tail of your picks spells out the required code sequence.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private static readonly string[] BreachAlphabet = ["1C", "55", "7A", "BD", "E9", "F2", "3D", "A0"];

    private void StartBreach(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var layer = ent.Comp.CurrentLayer;
        var size = Math.Clamp(4 + layer, 4, 6);
        var sequenceLength = 3 + layer;

        var path = GenerateBreachPath(size, sequenceLength);
        var sequence = Enumerable.Range(0, path.Count).Select(_ => _random.Pick(BreachAlphabet)).ToList();

        var codes = new List<string>(new string[size * size]);
        for (var i = 0; i < path.Count; i++)
        {
            codes[path[i]] = sequence[i];
        }
        for (var i = 0; i < codes.Count; i++)
        {
            codes[i] ??= _random.Pick(BreachAlphabet);
        }

        ent.Comp.BreachSize = size;
        ent.Comp.BreachCodes = codes;
        ent.Comp.BreachSequence = sequence;
        ent.Comp.BreachPath = new List<int>();
        ent.Comp.BreachMovesBudget = path.Count + 2 + layer;
        ent.Comp.BreachMovesLeft = ent.Comp.BreachMovesBudget;

        BeginTimer(ent, 20f + size * 3f);
    }

    // walks a legal column/row-alternating path of the given length, so the sequence
    // is guaranteed reachable, retries if it paints itself into a corner
    private List<int> GenerateBreachPath(int size, int length)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var path = new List<int> { _random.Next(size) };
            var row = 0;
            var col = path[0];
            var needColumn = true;

            while (path.Count < length)
            {
                var options = needColumn
                    ? Enumerable.Range(0, size).Where(r => r != row && !path.Contains(r * size + col)).ToList()
                    : Enumerable.Range(0, size).Where(c => c != col && !path.Contains(row * size + c)).ToList();

                if (options.Count == 0)
                    break;

                if (needColumn)
                    row = _random.Pick(options);
                else
                    col = _random.Pick(options);

                path.Add(row * size + col);
                needColumn = !needColumn;
            }

            if (path.Count == length)
                return path;
        }

        return GenerateBreachPath(size, Math.Max(1, length - 1));
    }

    private void HandleBreachClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        var size = ent.Comp.BreachSize;
        var path = ent.Comp.BreachPath;

        if (index < 0 || index >= size * size || path.Contains(index) || !IsLegalBreachMove(ent.Comp, index))
        {
            ApplyMistake(ent);
            return;
        }

        path.Add(index);
        ent.Comp.BreachMovesLeft--;
        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        if (MatchesSequenceTail(ent.Comp))
        {
            CompleteLayer(ent);
            return;
        }

        if (ent.Comp.BreachMovesLeft <= 0)
            RetryBreach(ent);
        else
            UpdateUi(ent);
    }

    private static bool IsLegalBreachMove(TechnologyDiskDecryptorComponent comp, int index)
    {
        var size = comp.BreachSize;
        var path = comp.BreachPath;

        if (path.Count == 0)
            return index / size == 0;

        var last = path[^1];
        var needColumn = path.Count % 2 == 1;
        return needColumn ? index % size == last % size : index / size == last / size;
    }

    private static bool MatchesSequenceTail(TechnologyDiskDecryptorComponent comp)
    {
        var seq = comp.BreachSequence;
        var path = comp.BreachPath;
        if (path.Count < seq.Count)
            return false;

        var offset = path.Count - seq.Count;
        for (var i = 0; i < seq.Count; i++)
        {
            if (comp.BreachCodes[path[offset + i]] != seq[i])
                return false;
        }

        return true;
    }

    private void RetryBreach(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        _audio.PlayPvs(ent.Comp.MistakeSound, ent);
        ent.Comp.BreachPath = new List<int>();
        ent.Comp.BreachMovesLeft = ent.Comp.BreachMovesBudget;
        UpdateUi(ent);
    }
}
