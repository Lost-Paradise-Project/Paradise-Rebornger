using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Prototypes;

/// <summary>
/// This is a prototype for a technology that can be unlocked.
/// </summary>
[Prototype]
public sealed partial class TechnologyPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name of the technology.
    /// Supports locale strings
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// An icon used to visually represent the technology in UI.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// What research discipline this technology belongs to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TechDisciplinePrototype> Discipline;

    /// <summary>
    /// What tier research is this?
    /// The tier governs how much lower-tier technology
    /// needs to be unlocked before this one.
    /// </summary>
    [DataField(required: true)]
    public int Tier;

    /// <summary>
    /// Hidden tech is not ever available at the research console.
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    /// How much research is needed to unlock.
    /// </summary>
    [DataField]
    public int Cost = 10000;

    /// <summary>
    /// A list of <see cref="TechnologyPrototype"/>s that need to be unlocked in order to unlock this technology.
    /// </summary>
    [DataField]
    public List<ProtoId<TechnologyPrototype>> TechnologyPrerequisites = new();

    /// <summary>
    /// A list of <see cref="LatheRecipePrototype"/>s that are unlocked by this technology
    /// </summary>
    [DataField]
    public List<ProtoId<LatheRecipePrototype>> RecipeUnlocks = new();

    /// <summary>
    /// A list of non-standard effects that are done when this technology is unlocked.
    /// </summary>
    [DataField]
    public IReadOnlyList<GenericUnlock> GenericUnlocks = new List<GenericUnlock>();

    // Starlight edit start
    /// <summary>
    /// Radio Channel to broadast on in addition to science when a technology of this discipline is unlocked.
    /// <\summary>
    [DataField("radioChannels", required: false)]
    public List<ProtoId<RadioChannelPrototype>> RadioChannels = [];
    // Starlight edit end

    /// Frontier: R&D console rework
    /// <summary>
    /// Position of this tech in console menu
    /// </summary>
    [DataField(required: true)]
    public Vector2i Position { get; private set; }

    /// <summary>
    /// Defines the visual style of prerequisite connection lines leading TO this technology.
    /// This controls how the lines from prerequisite techs to this tech are drawn.
    /// </summary>
    [DataField]
    public PrerequisiteLineType PrerequisiteLineType { get; private set; } = PrerequisiteLineType.LShape;

    /// <summary>
    /// Additional disciplines this technology belongs to.
    /// When specified, the technology will show a split color display.
    /// Limited to one additional discipline (total of 2 disciplines).
    /// </summary>
    [DataField]
    public ProtoId<TechDisciplinePrototype>? SecondaryDiscipline = null;

    /// <summary>
    /// Get all disciplines this technology belongs to.
    /// Returns primary discipline and secondary discipline if present.
    /// </summary>
    public List<ProtoId<TechDisciplinePrototype>> GetAllDisciplines()
    {
        var disciplines = new List<ProtoId<TechDisciplinePrototype>> { Discipline };
        if (SecondaryDiscipline.HasValue)
            disciplines.Add(SecondaryDiscipline.Value);
        return disciplines;
    }

    /// <summary>
    /// Check if this technology belongs to a specific discipline.
    /// </summary>
    public bool HasDiscipline(ProtoId<TechDisciplinePrototype> disciplineId)
    {
        return Discipline == disciplineId || (SecondaryDiscipline.HasValue && SecondaryDiscipline.Value == disciplineId);
    }
    /// End Frontier: R&D console rework
}

[DataDefinition]
public partial record struct GenericUnlock()
{
    /// <summary>
    /// What event is raised when this is unlocked?
    /// Used for doing non-standard logic.
    /// </summary>
    [DataField]
    public object? PurchaseEvent = null;

    /// <summary>
    /// A player facing tooltip for what the unlock does.
    /// Supports locale strings.
    /// </summary>
    [DataField]
    public string UnlockDescription = string.Empty;
}

// Frontier: This is used to define how the prerequisite lines are drawn in the R&D console UI.
/// <summary>
/// Defines the visual style of prerequisite connection lines
/// </summary>
public enum PrerequisiteLineType : byte
{
    /// <summary>
    /// Clean L-shaped connections (default)
    /// </summary>
    LShape = 0,

    /// <summary>
    /// Direct diagonal lines
    /// </summary>
    Diagonal = 1,

    /// <summary>
    /// Tree-like branching connections with structured hierarchy
    /// </summary>
    Tree = 2,

    /// <summary>
    /// Spread connections that avoid overlaps by using offset routing paths
    /// </summary>
    Spread = 3
}
// End Frontier
