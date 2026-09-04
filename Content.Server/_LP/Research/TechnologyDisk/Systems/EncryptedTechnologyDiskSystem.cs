using Content.Shared.Examine;
using Content.Shared.Research.Prototypes;
using Content.Shared._LP.Research.TechnologyDisk.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random.Helpers;

namespace Content.Server._LP.Research.TechnologyDisk.Systems;

public sealed class EncryptedTechnologyDiskSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EncryptedTechnologyDiskComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EncryptedTechnologyDiskComponent, ExaminedEvent>(OnExamine);
    }

    private void OnMapInit(Entity<EncryptedTechnologyDiskComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Recipes != null)
            return;

        int tier;
        if (ent.Comp.Tier.HasValue)
        {
            tier = ent.Comp.Tier.Value;
        }
        else
        {
            var weightedRandom = _protoMan.Index(ent.Comp.TierWeightPrototype);
            tier = int.Parse(weightedRandom.Pick(_random));
            ent.Comp.Tier = tier;
        }

        var bundles = new HashSet<(ProtoId<LatheRecipePrototype> recipe, ProtoId<TechDisciplinePrototype> discipline)>();
        foreach (var tech in _protoMan.EnumeratePrototypes<TechnologyPrototype>())
        {
            if (tech.Tier != tier)
                continue;
            if (ent.Comp.Discipline != null && tech.Discipline != ent.Comp.Discipline.Value)
                continue;

            foreach (var recipe in tech.RecipeUnlocks)
                bundles.Add((recipe, tech.Discipline));
        }

        if (bundles.Count == 0)
        {
            Log.Error($"Failed to roll a recipe for an encrypted technology disk of tier {tier}: no suitable recipes were found");
            return;
        }

        var bundle = _random.Pick(bundles);
        ent.Comp.Discipline = bundle.discipline;
        ent.Comp.Recipes = [bundle.recipe];
    }

    private void OnExamine(Entity<EncryptedTechnologyDiskComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("encrypted-tech-disk-examine"));
    }
}
