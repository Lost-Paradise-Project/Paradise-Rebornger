using System.Linq;
using Content.Shared._LP.Research.TechnologyDisk.Components;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

// Tier 2. Cycle each slot through a small palette of colors and submit a guess,
// feedback tells you how many are the right color in the right spot ("exact") and how
// many are the right color in the wrong spot ("partial"). Running out of attempts
// honestly (never an actual wrong action, just bad deduction) is a free re-roll of a
// brand new code, not an integrity hit.
public sealed partial class TechnologyDiskDecryptorSystem
{
    private void StartCodeGuess(Entity<TechnologyDiskDecryptorComponent> ent)
    {
        var layer = ent.Comp.CurrentLayer;
        var length = Math.Clamp(3 + layer, 3, 4);
        var colorCount = Math.Clamp(4 + layer, 4, 5);

        ent.Comp.CodeLength = length;
        ent.Comp.CodeColorCount = colorCount;
        ent.Comp.CodeSecret = Enumerable.Range(0, length).Select(_ => _random.Next(colorCount)).ToList();
        ent.Comp.CodeGuess = Enumerable.Repeat(0, length).ToList();
        ent.Comp.CodeAttemptsBudget = 10 + layer;
        ent.Comp.CodeAttemptsLeft = ent.Comp.CodeAttemptsBudget;
        ent.Comp.CodeLastExact = -1;
        ent.Comp.CodeLastPartial = -1;

        BeginTimer(ent, 15f + length * 6f);
    }

    private void HandleCodeGuessClick(Entity<TechnologyDiskDecryptorComponent> ent, int index)
    {
        if (index < 0 || index >= ent.Comp.CodeGuess.Count)
            return;

        ent.Comp.CodeGuess[index] = (ent.Comp.CodeGuess[index] + 1) % ent.Comp.CodeColorCount;
        _audio.PlayPvs(ent.Comp.NodeSound, ent);
        UpdateUi(ent);
    }

    private void OnSubmitCode(Entity<TechnologyDiskDecryptorComponent> ent, ref DiskDecryptorSubmitCodeMessage args)
    {
        if (ent.Comp.Phase != DecryptorPhase.Manual || ent.Comp.MinigameKind != MinigameKind.CodeGuess)
            return;

        var secret = ent.Comp.CodeSecret;
        var guess = ent.Comp.CodeGuess;
        var exact = 0;
        var secretLeftover = new List<int>();
        var guessLeftover = new List<int>();

        for (var i = 0; i < secret.Count; i++)
        {
            if (guess[i] == secret[i])
            {
                exact++;
            }
            else
            {
                secretLeftover.Add(secret[i]);
                guessLeftover.Add(guess[i]);
            }
        }

        var partial = 0;
        foreach (var color in guessLeftover)
        {
            var match = secretLeftover.IndexOf(color);
            if (match < 0)
                continue;

            secretLeftover.RemoveAt(match);
            partial++;
        }

        ent.Comp.CodeLastExact = exact;
        ent.Comp.CodeLastPartial = partial;
        _audio.PlayPvs(ent.Comp.NodeSound, ent);

        if (exact == secret.Count)
        {
            CompleteLayer(ent);
            return;
        }

        ent.Comp.CodeAttemptsLeft--;
        if (ent.Comp.CodeAttemptsLeft <= 0)
        {
            _audio.PlayPvs(ent.Comp.MistakeSound, ent);
            StartCodeGuess(ent); // out of honest attempts - fresh code, no integrity cost
            return;
        }

        UpdateUi(ent);
    }
}
