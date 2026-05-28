namespace Content.Shared._LP.Containers;

/// <summary>
/// Replenishes with a certain time interval the contents of things based on EntityTableContainerFill or by adding something else.
/// </summary>
[RegisterComponent]
public sealed partial class EntityTableRestockComponent : Component
{
    [DataField]
    public TimeSpan RestockTime = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan Accumulator = TimeSpan.Zero;

    /// <summary>
    /// If true:
    /// Use content of EntityTableContainerFill again.
    /// If false:
    /// Use EntitiesToSpawn.
    /// </summary>
    [DataField]
    public bool UseEntityTable = true;

    /// <summary>
    /// Entities to spawn instead of using past content of EntityTable.
    /// </summary>
    [DataField]
    public List<string> EntitiesToSpawn = new();

    /// <summary>
    /// If container has something in it then DELETE old contents and replace them.
    /// </summary>
    [DataField]
    public bool ReplaceContents = false;
}
