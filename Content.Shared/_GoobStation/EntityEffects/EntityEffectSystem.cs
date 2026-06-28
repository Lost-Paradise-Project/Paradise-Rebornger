using Robust.Shared.Timing;
using Content.Shared.EntityEffects;

namespace Content.Shared._GoobStation.EntityEffects;

/// <summary>
///     (Goobstation) - Entity Effect System. Use this instead of manually calling effects.
/// </summary>

// this should've been done a long time ago ngl.
// also lowkey this is not worth the goobstation folder so i'll just leave it here.
public sealed partial class SharedEntityEffectSystem : EntitySystem
{
    public struct EntityEffectQueueEntry
    {
        public TimeSpan Time;
        public EntityUid Target;
        public EntityEffect Effect;
        public float Scale;
        public EntityUid? User;

        public EntityEffectQueueEntry(TimeSpan time, EntityUid target, EntityEffect effect, float scale, EntityUid? user)
        {
            Time = time;
            Target = target;
            Effect = effect;
            Scale = scale;
            User = user;
        }
    }

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    private List<EntityEffectQueueEntry> _queue = new();

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        for (int i = 0; i < _queue.Count; i++)
        {
            var item = _queue[i];
            if (item.Time <= _timing.CurTime)
            {
                _effects.ApplyEffect(item.Target, item.Effect, item.Scale, item.User);
                _queue.RemoveAt(i);
                i--; // index dont move
            }
        }
    }

    public void Effect(EntityUid target, EntityEffect effect, float scale = 1f, EntityUid? user = null)
        => Effect(target, effect, effect.Delay, scale, user);

    public void Effect(EntityUid target, EntityEffect effect, TimeSpan delay, float scale = 1f, EntityUid? user = null)
    {
        if (delay.TotalMilliseconds <= 0)
        {
            _effects.ApplyEffect(target, effect, scale, user);
            return;
        }

        _queue.Add(new EntityEffectQueueEntry(_timing.CurTime + delay, target, effect, scale, user));
    }
}
