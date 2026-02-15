using System.Numerics;
using Content.Shared.Projectiles;
using Content.Shared._LP.Supermatter.Reflector.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._LP.Supermatter.Reflector;

public sealed class ReflectorSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReflectorComponent, ProjectileReflectAttemptEvent>(OnProjectileReflect);
    }

    private void OnProjectileReflect(Entity<ReflectorComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryReflectProjectile(ent, args.ProjUid))
            args.Cancelled = true;
    }

    private bool TryReflectProjectile(Entity<ReflectorComponent> reflector, Entity<ProjectileComponent?> projectile)
    {
        if (!TryComp<ReflectorTargetComponent>(projectile, out var reflective) ||
            (reflector.Comp.Reflects & reflective.Reflective) == 0x0 ||
            !TryComp<PhysicsComponent>(projectile, out var physics))
        {
            return false;
        }

        var existingVelocity = _physics.GetMapLinearVelocity(projectile, physics);
        if (existingVelocity.LengthSquared() <= 0.001f)
            return false;

        var incomingDir = existingVelocity.Normalized();

        var reflectedDir = CalculateReflectionDirection(reflector, incomingDir);

        var reflectorTransform = Transform(reflector);
        _transform.SetWorldPosition(projectile, reflectorTransform.WorldPosition);

        var rotation = reflectedDir.ToAngle() - incomingDir.ToAngle();

        var newVelocity = rotation.RotateVec(existingVelocity);
        var difference = newVelocity - existingVelocity;

        _physics.SetLinearVelocity(projectile, physics.LinearVelocity + difference, body: physics);

        var locRot = Transform(projectile).LocalRotation;
        _transform.SetLocalRotation(projectile, locRot + rotation);

        PlaySound(reflector);
        return true;
    }


    /// <summary>
    /// (en-En)CalculateReflectionDirection calculates the final direction of the projectile depending on which mode is set in the reflector component.
    /// (ru-Ru)CalculateReflectionDirection вычисляет конечное направление снаряда в зависимости от того какой режим установлен в компоненте рефлектора
    /// </summary>
    /// <param name="reflector"></param>
    /// <param name="incoming"></param>
    /// <returns>
    /// (en-En)The direction of the projectile, taking into account the applied reflection.
    /// (ru-Ru)Направление снаряда с учётом применённого отражения.
    /// </returns>
    private Vector2 CalculateReflectionDirection(Entity<ReflectorComponent> reflector, Vector2 incoming)
    {
        var rotation = Transform(reflector).WorldRotation;

        Vector2 baseDir = reflector.Comp.DirectionMode switch
        {
            ReflectorDirection.Forward => rotation.ToWorldVec(),
            ReflectorDirection.Backward => -rotation.ToWorldVec(),
            ReflectorDirection.Left => rotation.RotateVec(new Vector2(-1, 0)),
            ReflectorDirection.Right => rotation.RotateVec(new Vector2(1, 0)),
            ReflectorDirection.Mirror => Vector2.Reflect(incoming, rotation.ToWorldVec()),
            _ => incoming
        };

        baseDir = baseDir.Normalized();
        baseDir = reflector.Comp.AngleOffset.RotateVec(baseDir);

        return baseDir;
    }

    private void PlaySound(Entity<ReflectorComponent> reflector)
    {
        if (_net.IsServer && reflector.Comp.SoundOnReflect != null)
            _audio.PlayPvs(reflector.Comp.SoundOnReflect, reflector.Owner);
    }
}
