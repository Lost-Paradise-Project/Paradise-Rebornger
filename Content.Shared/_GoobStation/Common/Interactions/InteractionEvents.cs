using Robust.Shared.GameObjects;

namespace Content.Shared._GoobStation.Common.Interactions;

/// <summary>
///     UseAttempt, but for item.
/// </summary>
public sealed class UseInHandAttemptEvent(EntityUid user) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = user;
}
