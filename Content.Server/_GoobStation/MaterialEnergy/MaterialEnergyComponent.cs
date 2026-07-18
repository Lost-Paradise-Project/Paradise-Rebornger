namespace Content.Server._GoobStation.MaterialEnergy;

[RegisterComponent]
public sealed partial class MaterialEnergyComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string>? MaterialWhiteList;

    // LP Edit Start, player can now use low quality materials to charge the plasma cutter, but at half efficiency

    [DataField, AutoNetworkedField]
    public List<string>? LowQualityMaterialWhiteList;
    // LP Edit End, player can now use low quality materials to charge the plasma cutter, but at half efficiency
}
