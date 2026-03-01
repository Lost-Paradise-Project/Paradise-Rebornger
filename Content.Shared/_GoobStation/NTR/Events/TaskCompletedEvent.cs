namespace Content.Shared._GoobStation.NTR.Events;
public sealed class TaskCompletedEvent : EntityEventArgs
{
    public NtrTaskData Task;

    public TaskCompletedEvent(NtrTaskData task)
    {
        Task = task;
    }
}
