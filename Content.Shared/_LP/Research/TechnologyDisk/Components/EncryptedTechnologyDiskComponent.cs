using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.Random;

namespace Content.Shared._LP.Research.TechnologyDisk.Components;

/// <summary>
/// Marks an item as an encrypted technology disk: a random recipe of a random tier is
/// rolled on spawn and hidden inside, only obtainable by decrypting it at a
/// TechnologyDiskDecryptor. Deliberately NOT networked/AutoGenerateComponentState - the
/// tier and recipe must stay server-authoritative so a client can't inspect the entity
/// state to learn the answer before decrypting.
/// </summary>
[RegisterComponent]
public sealed partial class EncryptedTechnologyDiskComponent : Component
{
    [DataField]
    public ProtoId<TechDisciplinePrototype>? Discipline;

    [DataField]
    public int? Tier;

    [DataField]
    public List<ProtoId<LatheRecipePrototype>>? Recipes;

    [DataField]
    public ProtoId<WeightedRandomPrototype> TierWeightPrototype = "TechDiskTierWeights";
}
