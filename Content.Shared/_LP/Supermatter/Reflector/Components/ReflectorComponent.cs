using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Weapons.Reflect;

namespace Content.Shared._LP.Supermatter.Reflector.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReflectorComponent : Component
{
    /// <summary>
    /// What types of projectiles this reflector affects.
    /// </summary>
    [DataField]
    public ReflectType Reflects = ReflectType.Energy | ReflectType.NonEnergy;

    /// <summary>
    /// Selected reflection direction.
    /// </summary>
    [DataField("ReflectingMode"), AutoNetworkedField]
    public ReflectorDirection DirectionMode = ReflectorDirection.Mirror;

    /// <summary>
    /// Additional angle offset applied after direction calculation.
    /// </summary>
    [DataField("ReflectingAngle"), AutoNetworkedField]
    public Angle AngleOffset = Angle.Zero;

    /// <summary>
    /// Sound played on reflection.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundOnReflect =
        new SoundPathSpecifier("/Audio/_LP/Supermatter/Reflector/reflector.ogg");
}

[Serializable, NetSerializable]
public enum ReflectorDirection : byte
{
    Forward,
    Backward,
    Left,
    Right,
    Mirror
}
