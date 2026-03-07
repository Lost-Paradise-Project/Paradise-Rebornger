namespace Content.Shared._GoobStation.NTR.Events;

public sealed class TaskFailedEvent(EntityUid user, int penalty = 4) : EntityEventArgs
{
    public EntityUid User = user;
    public int Penalty = penalty;
}
