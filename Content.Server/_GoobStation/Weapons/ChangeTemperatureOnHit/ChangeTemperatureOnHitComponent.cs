namespace Content.Server._GoobStation.Weapons.ChangeTemperatureOnHit;

[RegisterComponent]
public sealed partial class ChangeTemperatureOnHitComponent : Component
{
    [DataField]
    public float Heat;

    [DataField]
    public bool IgnoreResistances = true;
}
