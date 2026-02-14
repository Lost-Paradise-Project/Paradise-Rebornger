using Content.Shared._LP.Supermatter.Reflector.Components;
using Content.Shared._LP.Supermatter.Reflector;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.GameStates;

namespace Content.Shared._LP.Supermatter.Reflector.Components;

/// <summary>
/// Can this entity be reflected by reflector.
/// Only applies if it is shot like a projectile and not if it is thrown.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReflectorTargetComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public ReflectType Reflective = ReflectType.NonEnergy;
}
