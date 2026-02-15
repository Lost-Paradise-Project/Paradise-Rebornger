namespace Content.Shared._GoobStation.Singularity;

/// <summary>
/// Raised on an entity that just collided with a containment field
/// </summary>
[ByRefEvent]
public record struct ContainmentFieldThrowEvent(EntityUid Field, bool Cancelled = false);
