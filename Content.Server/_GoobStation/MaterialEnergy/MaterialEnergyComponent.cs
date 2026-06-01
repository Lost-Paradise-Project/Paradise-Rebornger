namespace Content.Server._GoobStation.MaterialEnergy;

[RegisterComponent]
public sealed partial class MaterialEnergyComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string>? MaterialWhiteList;
}
