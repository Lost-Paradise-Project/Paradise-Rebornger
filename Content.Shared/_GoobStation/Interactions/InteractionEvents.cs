using Robust.Shared.GameObjects;

namespace Content.Shared._GoobStation.Interactions;

/// <summary>
///     UseAttempt, but for item.
/// </summary>
public sealed class UseInHandAttemptEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
}
