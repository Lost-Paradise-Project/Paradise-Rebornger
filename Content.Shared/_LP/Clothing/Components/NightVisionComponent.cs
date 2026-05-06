using Content.Shared._LP.Clothing.Systems;
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared._LP.Clothing.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class NightVisionComponent : Component
{
    /// <summary>
    /// IsAnimal NOT WORKING RIGHT NOW, CHANGE IN NEXT UPDATE
    /// </summary>
    [DataField("animal")]
    public bool IsAnimal = false;

    [DataField]
    public string? RequiredSlot;

    [DataField]
    public float Tint1 { get; set; } = 0.3f;

    [DataField]
    public float Tint2 { get; set; } = 0.3f;

    [DataField]
    public float Tint3 { get; set; } = 0.3f;

    public Vector3 Tint
    {
        get => new(Tint1, Tint2, Tint3);
        set
        {
            Tint1 = value.X;
            Tint2 = value.Y;
            Tint3 = value.Z;
        }
    }

    [DataField]
    public float Strength = 2f;

    [DataField]
    public float Noise = 0.03f;

    [DataField]
    public float Scanline = 0.01f;

    [DataField]
    public float Vignette = 1.25f;

    [DataField]
    public float Flicker = 2.0f;
}
